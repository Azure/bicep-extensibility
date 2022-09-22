// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core;
using Azure.Deployments.Extensibility.Core.Json;
using Azure.Deployments.Extensibility.Providers.ThirdParty.ACI;
using Azure.Deployments.Extensibility.Providers.ThirdParty.AzureContext;
using Json.More;
using Json.Pointer;
using System;
using System.Net.Mime;
using System.Text;
using System.Text.Json;

namespace Azure.Deployments.Extensibility.Providers.ThirdParty
{
    internal record ExtensibilityProviderContainerRegistry(string ContainerRegistry, int ExternalPort);

    public interface IThirdPartyExtensibilityProvider : IExtensibilityProvider
    {
    }

    internal class ThirdPartyExtensibilityProvider : IThirdPartyExtensibilityProvider
    {
        private static string ContainerInstanceResourceGroupName => "bicep-extensibility-dev-host-rg";

        private readonly IAzureContainerInstanceHost containerInstanceHost;

        public ThirdPartyExtensibilityProvider(IAzureContainerInstanceHost containerInstanceHost)
        {
            this.containerInstanceHost = containerInstanceHost;
        }

        public Task<ExtensibilityOperationResponse> GetAsync(ExtensibilityOperationRequest request, CancellationToken cancellationToken) =>
            this.HandleRequestByContainerRegistryAsync(operation: "get", request: request, cancellation: cancellationToken);

        public Task<ExtensibilityOperationResponse> PreviewSaveAsync(ExtensibilityOperationRequest request, CancellationToken cancellationToken) =>
            this.HandleRequestByContainerRegistryAsync(operation: "previewSave", request: request, cancellation: cancellationToken);

        public Task<ExtensibilityOperationResponse> SaveAsync(ExtensibilityOperationRequest request, CancellationToken cancellationToken) =>
            this.HandleRequestByContainerRegistryAsync(operation: "save", request: request, cancellation: cancellationToken);

        public Task<ExtensibilityOperationResponse> DeleteAsync(ExtensibilityOperationRequest request, CancellationToken cancellationToken) =>
            this.HandleRequestByContainerRegistryAsync(operation: "delete", request: request, cancellation: cancellationToken);


        private async Task <ExtensibilityProviderContainerRegistry?> TryGetExtensibilityProviderContainerRegistry(string providerName)
        {
            if (providerName != "github")
            {
                // TODO check the registry dynamically here
                await Task.Yield();
                return null;
            }

            return new ExtensibilityProviderContainerRegistry(ContainerRegistry: $"bicepprovidersregistry.azurecr.io/{providerName}/server", ExternalPort: 8080);
        }

        private async Task<ExtensibilityOperationResponse> HandleRequestByContainerRegistryAsync(
            string operation,
            ExtensibilityOperationRequest request,
            CancellationToken cancellation)
        {
            var providerName = request.Import.Provider;
            var tag = request.Import.Version;
            var containerGroupName = GenerateContainerGroupName(providerName, tag);

            var providerContainerRegistry = await TryGetExtensibilityProviderContainerRegistry(providerName);

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

        private static async Task<ExtensibilityOperationResponse> CallExtensibilityProviderAsync(
            Uri uri,
            ExtensibilityOperationRequest request,
            CancellationToken cancellation)
        {
            var response = await CallAsync(uri, request, cancellation);
            return ExtensibilityJsonSerializer.Default.Deserialize<ExtensibilityOperationResponse>(await response.Content.ReadAsStreamAsync())
                ?? throw new InvalidOperationException($"Failed to deserialize response from provider.");
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
	
        private static string GenerateContainerGroupName(string providerName, string providerVersion)
        {
            var desiredName = $"{providerName}-{providerVersion}".ToLowerInvariant();

            return new string(desiredName.Select(x => char.IsLetterOrDigit(x) ? x : '-').ToArray());
        }
    }
}
