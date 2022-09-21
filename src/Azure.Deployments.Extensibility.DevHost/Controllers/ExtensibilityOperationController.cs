// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core;
using Azure.Deployments.Extensibility.DevHost.ACI;
using Azure.Deployments.Extensibility.DevHost.AzureContext;
using Azure.Deployments.Extensibility.DevHost.Swagger;
using Json.More;
using Json.Pointer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.KeyVault.WebKey;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;
using System;
using System.Net.Mime;
using System.Text;

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

        private IAzureContainerInstanceHost ContainerInstanceHost { get; }

        public ExtensibilityOperationController(IExtensibilityProviderRegistry registry, IAzureContainerInstanceHost containerInstanceHost)
        {
            this.registry = registry;
            this.ContainerInstanceHost = containerInstanceHost;
        }

        [HttpPost("/get")]
        [SwaggerOperation("Get operation", "Gets an extensible resource")]
        [SwaggerResponse(200, Type = typeof(ExtensibilityOperationResponse), ContentTypes = new[] { "application/json" })]
        [SwaggerResponseExample(200, typeof(ExtensibilityOperationResponseExampleProvider))]
        public Task<object> GetAsync(ExtensibilityOperationRequest request, CancellationToken cancellation) =>
            this.HandleRequestByContainerRegistryAsync(
                api: "get",
                request: request,
                cancellation);
        //return await this.HandleRequestAsync(request, provider => provider.GetAsync, cancellation);

        [HttpPost("/previewSave")]
        [SwaggerOperation("PreviewSave operation", "Previews the result of saving an extensible resource")]
        [SwaggerResponse(200, Type = typeof(ExtensibilityOperationResponse), ContentTypes = new[] { "application/json" })]
        [SwaggerResponseExample(200, typeof(PreviewSaveResponseExampleProvider))]
        public Task<object> PreviewSaveAsync(ExtensibilityOperationRequest request, CancellationToken cancellation) =>
            this.HandleRequestAsync(request, provider => provider.PreviewSaveAsync, cancellation);


        [HttpPost("/save")]
        [SwaggerOperation("Save operation", "Saves an extensible resource")]
        [SwaggerResponse(200, Type = typeof(ExtensibilityOperationResponse), ContentTypes = new[] { "application/json" })]
        [SwaggerResponseExample(200, typeof(ExtensibilityOperationResponseExampleProvider))]
        public Task<object> SaveAsync(ExtensibilityOperationRequest request, CancellationToken cancellation) =>
            this.HandleRequestAsync(request, provider => provider.SaveAsync, cancellation);

        [HttpPost("/delete")]
        [SwaggerOperation("Delete operation", "Deletes an extensible resource")]
        [SwaggerResponse(200, Type = typeof(ExtensibilityOperationResponse), ContentTypes = new[] { "application/json" })]
        [SwaggerResponseExample(200, typeof(ExtensibilityOperationResponseExampleProvider))]
        public Task<object> DeleteAsync(ExtensibilityOperationRequest request, CancellationToken cancellation) =>
            this.HandleRequestAsync(request, provider => provider.DeleteAsync, cancellation);

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
            string api,
            ExtensibilityOperationRequest request,
            CancellationToken cancellation)
        {
            var providerName = request.Import.Provider;
            var tag = request.Import.Version;

            // TODO: External port should come from Import properties? Probably the registry dict?
            var externalPort = 8080;
            var providerContainerRegistry = registry.TryGetExtensibilityProviderContainerRegistry(providerName);

            if (providerContainerRegistry is null)
            {
                return new ExtensibilityOperationErrorResponse(
                    new ExtensibilityError(
                        "UnknownExtensibilityProvider",
                        JsonPointer.Parse($"/imports/provider"),
                        @$"Unknown extensibility provider: ""{providerName}""."));
            }

            var image = string.Format(providerContainerRegistry, tag);
            var fqdn = await CreateContainerGroupAsync(containerGroupPrefix: "github", image: image, externalPort: externalPort, cancellation: cancellation);
            var uri = new Uri($"http://{fqdn}:{externalPort}/{api}");

            var response = await CallAsync(uri, request, cancellation);
            var responseJToken = await FromJson<JToken>(response.Content);
            var errors = responseJToken.SelectToken("errors");

            if (errors is JArray jArray && jArray.Count > 0)
            {
                return new ExtensibilityOperationErrorResponse(Errors: GetErrors(jArray));
            }

            return responseJToken.ToObject<ExtensibilityOperationSuccessResponse>();
        }

        private IEnumerable<ExtensibilityError> GetErrors(JArray errors)
        {
            foreach (var error in errors)
            {
                yield return new ExtensibilityError(
                    Code: error.SelectToken("code")?.Value<string>() ?? string.Empty,
                    Target: error.SelectToken("target") != null ? JsonPointer.Parse(error.SelectToken("target").Value<string>()) : JsonPointer.Parse(string.Empty),
                    Message: error.SelectToken("message")?.Value<string>() ?? string.Empty);
            }
        }

        private static async Task<TType> FromJson<TType>(HttpContent content)
        {
            using var streamReader = new StreamReader(
                stream: await content.ReadAsStreamAsync(),
                encoding: Encoding.UTF8,
                detectEncodingFromByteOrderMarks: true);
            using var jsonTextReader = new JsonTextReader(streamReader);
            var serializer = new JsonSerializer();
            return serializer.Deserialize<TType>(jsonTextReader);
        }

        private static async Task<HttpResponseMessage> CallAsync(Uri uri, ExtensibilityOperationRequest request, CancellationToken cancellation)
        {
            using var client = new HttpClient();
            var requestJson = JsonConvert.SerializeObject(request);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = new StringContent(content: requestJson, encoding: Encoding.UTF8, mediaType: "application/json"),
            };

            return await client.SendAsync(request: requestMessage, cancellationToken: cancellation);
        }

        private async Task<string> CreateContainerGroupAsync(string containerGroupPrefix, string image, int externalPort, CancellationToken cancellation)
            => await ContainerInstanceHost.CreateContainerGroupAsync(
                    resourceGroupName: ContainerInstanceResourceGroupName,
                    containerGroupPrefix: containerGroupPrefix,
                    image: image,
                    externalPort: externalPort,
                    cancellation: cancellation);
    }
}
