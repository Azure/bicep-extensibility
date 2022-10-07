// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Containers.ContainerRegistry;
using Azure.Deployments.Extensibility.Core;
using Azure.Deployments.Extensibility.Core.Json;
using Json.Pointer;
using System.Text;
using System.Text.Json;

namespace Azure.Deployments.Extensibility.Providers.ThirdParty;

internal record ExtensibilityProviderContainerRegistry(string ContainerRegistry, int ExternalPort);

public interface IThirdPartyExtensibilityProvider : IExtensibilityProvider
{
}

internal class ThirdPartyExtensibilityProvider : IThirdPartyExtensibilityProvider
{
    private const string ContainerRegistryHostname = "bicepprovidersregistry.azurecr.io";
    private const int ExtensibilityContainerPort = 8080;

    private readonly IContainerManager containerManager;

    public ThirdPartyExtensibilityProvider(IContainerManager containerManager)
    {
        this.containerManager = containerManager;
    }

    public Task<object> GetAsync(ExtensibilityOperationRequest request, CancellationToken cancellationToken) =>
        this.HandleRequestByContainerRegistryAsync(operation: "get", request: request, cancellation: cancellationToken);

    public Task<object> PreviewSaveAsync(ExtensibilityOperationRequest request, CancellationToken cancellationToken) =>
        this.HandleRequestByContainerRegistryAsync(operation: "previewSave", request: request, cancellation: cancellationToken);

    public Task<object> SaveAsync(ExtensibilityOperationRequest request, CancellationToken cancellationToken) =>
        this.HandleRequestByContainerRegistryAsync(operation: "save", request: request, cancellation: cancellationToken);

    public Task<object> DeleteAsync(ExtensibilityOperationRequest request, CancellationToken cancellationToken) =>
        this.HandleRequestByContainerRegistryAsync(operation: "delete", request: request, cancellation: cancellationToken);


    private async Task<ExtensibilityProviderContainerRegistry?> TryGetExtensibilityProviderContainerRegistry(string providerName, string tag, CancellationToken cancellation)
    {
        var client = new ContainerRegistryClient(new($"https://{ContainerRegistryHostname}"), new ContainerRegistryClientOptions
        {
            Audience = ContainerRegistryAudience.AzureResourceManagerPublicCloud,
        });

        var serverRepo = $"{providerName}/server";
        try
        {
            await client.GetRepository(serverRepo).GetArtifact(tag).GetManifestPropertiesAsync(cancellation);

            return new ExtensibilityProviderContainerRegistry(ContainerRegistry: $"{ContainerRegistryHostname}/{serverRepo}", ExternalPort: ExtensibilityContainerPort);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    private async Task<object> HandleRequestByContainerRegistryAsync(
        string operation,
        ExtensibilityOperationRequest request,
        CancellationToken cancellation)
    {
        var providerName = request.Import.Provider;
        var tag = request.Import.Version;
        var containerGroupName = GenerateContainerGroupName(providerName, tag);

        var providerContainerRegistry = await TryGetExtensibilityProviderContainerRegistry(providerName, tag, cancellation);

        if (providerContainerRegistry is null)
        {
            return new ExtensibilityOperationErrorResponse(
                new ExtensibilityError(
                    "UnknownExtensibilityProvider",
                    JsonPointer.Parse("/imports"),
                    @$"Unknown extensibility provider ""{providerName}"" with version ""{tag}""."));
        }

        try
        {
            var externalPort = providerContainerRegistry.ExternalPort;
            var image = $"{providerContainerRegistry.ContainerRegistry}:{tag}";
            var baseUri = await containerManager.Create(containerGroupName, image, externalPort, cancellation);
            var operationUri = new Uri(baseUri, operation);

            return await CallExtensibilityProviderAsync(operationUri, request, cancellation);
        }
        catch (Exception ex)
        {
            // TODO: Handle container provisioning failures
            return new ExtensibilityOperationErrorResponse(
                    new ExtensibilityError(
                        "ExtensibilityProviderRequestError",
                        JsonPointer.Parse(string.Empty),
                        ex.Message));
        }
        finally
        {
            // TODO: Tear down container
            // await containerManager.Delete(containerGroupName, cancellation);
        }
    }

    private static async Task<JsonElement> CallExtensibilityProviderAsync(
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

    private static string GenerateContainerGroupName(string providerName, string providerVersion)
    {
        var desiredName = $"{providerName}-{providerVersion}".ToLowerInvariant();

        return new string(desiredName.Select(x => char.IsLetterOrDigit(x) ? x : '-').ToArray());
    }
}
