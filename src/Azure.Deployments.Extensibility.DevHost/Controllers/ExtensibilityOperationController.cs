// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core;
using Azure.Deployments.Extensibility.Core.Json;
using Azure.Deployments.Extensibility.DevHost.ACI;
using Azure.Deployments.Extensibility.DevHost.AzureContext;
using Azure.Deployments.Extensibility.DevHost.Swagger;
using Json.More;
using Json.Pointer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.KeyVault.WebKey;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;
using System;
using System.Net.Mime;
using System.Text;
using System.Text.Json;

namespace Azure.Deployments.Extensibility.DevHost.Controllers
{
    [ApiController]
    [Tags("Resource operations")]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    public class ExtensibilityOperationController
    {
        private readonly IExtensibilityProviderRegistry registry;

        private static string ContainerInstanceResourceGroupName => "bicep-extensibility-dev-host-rg";

        private readonly IAzureContainerInstanceHost containerInstanceHost;

        private delegate Task<ExtensibilityOperationResponse> HandleRequestByContainerRegistryCallback(Uri uri, ExtensibilityOperationRequest request, CancellationToken cancellation);

        public ExtensibilityOperationController(IExtensibilityProviderRegistry registry, IAzureContainerInstanceHost containerInstanceHost)
        {
            this.registry = registry;
            this.containerInstanceHost = containerInstanceHost;
        }

        [HttpPost("/get")]
        [SwaggerOperation("Get operation", "Gets an extensible resource")]
        [SwaggerResponse(200, Type = typeof(ExtensibilityOperationResponse), ContentTypes = new[] { "application/json" })]
        [SwaggerResponseExample(200, typeof(ExtensibilityOperationResponseExampleProvider))]
        public Task<object> GetAsync(ExtensibilityOperationRequest request, CancellationToken cancellation) =>
            this.HandleRequestByContainerRegistryAsync(operation: "get", request: request, cancellation: cancellation);

        [HttpPost("/previewSave")]
        [SwaggerOperation("PreviewSave operation", "Previews the result of saving an extensible resource")]
        [SwaggerResponse(200, Type = typeof(ExtensibilityOperationResponse), ContentTypes = new[] { "application/json" })]
        [SwaggerResponseExample(200, typeof(PreviewSaveResponseExampleProvider))]
        public Task<object> PreviewSaveAsync(ExtensibilityOperationRequest request, CancellationToken cancellation) =>
            this.HandleRequestByContainerRegistryAsync(operation: "previewSave", request: request, cancellation: cancellation);


        [HttpPost("/save")]
        [SwaggerOperation("Save operation", "Saves an extensible resource")]
        [SwaggerResponse(200, Type = typeof(ExtensibilityOperationResponse), ContentTypes = new[] { "application/json" })]
        [SwaggerResponseExample(200, typeof(ExtensibilityOperationResponseExampleProvider))]
        public Task<object> SaveAsync(ExtensibilityOperationRequest request, CancellationToken cancellation) =>
            this.HandleRequestByContainerRegistryAsync(operation: "save", request: request, cancellation: cancellation);

        [HttpPost("/delete")]
        [SwaggerOperation("Delete operation", "Deletes an extensible resource")]
        [SwaggerResponse(200, Type = typeof(ExtensibilityOperationResponse), ContentTypes = new[] { "application/json" })]
        [SwaggerResponseExample(200, typeof(ExtensibilityOperationResponseExampleProvider))]
        public Task<object> DeleteAsync(ExtensibilityOperationRequest request, CancellationToken cancellation) =>
            this.HandleRequestByContainerRegistryAsync(operation: "delete", request: request, cancellation: cancellation);

        private async Task<object> HandleRequestAsync(ExtensibilityOperationRequest request, Func<IExtensibilityProvider, ExtensibilityOperation> operationSelector, CancellationToken cancellationToken)
        {
            var providerName = request.Import.Provider;
            var provider = registry.TryGetExtensibilityProvider(providerName);

            if (provider is null)
            {
                return new ExtensibilityOperationErrorResponse(
                    new ExtensibilityError(
                        "UnknownExtensibilityProvider",
                        JsonPointer.Parse($"/imports/provider"),
                        @$"Unknown extensibility provider: ""{providerName}""."));
            }

            var operation = operationSelector.Invoke(provider);
            var response = await operation.Invoke(request, cancellationToken);

            return response;
        }

        private async Task<object> HandleRequestByContainerRegistryAsync(
            string operation,
            ExtensibilityOperationRequest request,
            CancellationToken cancellation)
        {
            var providerName = request.Import.Provider;
            var tag = request.Import.Version;
            var containerGroupName = $"{providerName}-aci";

            var providerContainerRegistry = registry.TryGetExtensibilityProviderContainerRegistry(providerName);

            if (providerContainerRegistry is null)
            {
                return new ExtensibilityOperationErrorResponse(
                    new ExtensibilityError(
                        "UnknownExtensibilityProvider",
                        JsonPointer.Parse("/imports/provider"),
                        @$"Unknown extensibility provider: ""{providerName}""."));
            }

            try
            {
                var externalPort = providerContainerRegistry.ExternalPort;
                var image = $"{providerContainerRegistry.ContainerRegistry}:{tag}";
                var fqdn = await CreateContainerGroupAsync(containerGroupName: containerGroupName, image: image, externalPort: externalPort, cancellation: cancellation);
                var uri = new Uri($"http://{fqdn}:{externalPort}/{operation}");

                return await CallExtensibilityProviderAsync(uri: uri, request: request, cancellation: cancellation);
            }
            catch (Exception ex)
            {
                // TODO: Handle ACI provisioning failures
                return new ExtensibilityOperationErrorResponse(
                        new ExtensibilityError(
                            "ExtensibilityProviderRequestError",
                            JsonPointer.Parse(string.Empty),
                            ex.Message));
            }
            finally
            {
                // TODO: Tear down ACI
            }
        }

        private static async Task<object> CallExtensibilityProviderAsync(
            Uri uri,
            ExtensibilityOperationRequest request,
            CancellationToken cancellation)
        {
            var response = await CallAsync(uri, request, cancellation);
            return ExtensibilityJsonSerializer.Default.Deserialize<JsonElement>(await response.Content.ReadAsStreamAsync());
        }

        private static async Task<HttpResponseMessage> CallAsync(Uri uri, ExtensibilityOperationRequest request, CancellationToken cancellation)
        {
            using var client = new HttpClient();
            var requestJson = ExtensibilityJsonSerializer.Default.Serialize(request);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = new StringContent(content: requestJson, encoding: Encoding.UTF8, mediaType: "application/json"),
            };

            return await client.SendAsync(request: requestMessage, cancellationToken: cancellation);
        }

        private async Task<string> CreateContainerGroupAsync(string containerGroupName, string image, int externalPort, CancellationToken cancellation)
            => await containerInstanceHost.CreateContainerGroupAsync(
                    resourceGroupName: ContainerInstanceResourceGroupName,
                    containerGroupName: containerGroupName,
                    image: image,
                    externalPort: externalPort,
                    cancellation: cancellation);

        private async Task DeleteContainerGroupAsync(string containerGroupName, CancellationToken cancellation)
            => await containerInstanceHost.DeleteContainerGroupAsync(
                    resourceGroupName: ContainerInstanceResourceGroupName,
                    containerGroupName: containerGroupName,
                    cancellation: cancellation);
    }
}
