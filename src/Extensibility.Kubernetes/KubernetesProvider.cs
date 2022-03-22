using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Extensibility.Core.Contract;
using Extensibility.Core.Data;
using Extensibility.Core.Messages;
using k8s;
using Microsoft.Rest;

namespace Extensibility.Kubernetes
{
    public class KubernetesProvider : IExtensibilityProvider
    {
        public async Task<DeleteResponse> Delete(DeleteRequest request, CancellationToken cancellationToken)
        {
            var resource = request?.Body ?? throw new ArgumentNullException($"Request body not provided");

            var (name, @namespace) = GetKeys(resource);
            if (!GroupVersionKind.TryParse(resource.Type!, out var gvk))
            {
                throw new InvalidOperationException($"type: {resource.Type} is not a valid kubernetes type.");
            }

            var (config, clientConfig) = InitializeConfig(resource.Import!);

            // HEY LISTEN: We might want a way to reuse resources across operations. This will open a
            // connection to the Kubernetes cluster PER-operation. 
            using var client = new CoolKubernetesClient(clientConfig);
            var api = await GetApiResourceAsync(client, gvk.Value, cancellationToken);

            if (api.Namespaced)
            {
                try
                {
                    var response = await client.DeleteNamespacedCustomObjectWithHttpMessagesAsync(
                        api.Group ?? "",
                        api.Version,
                        @namespace ?? config.Namespace,
                        api.Name,
                        name,
                        cancellationToken: cancellationToken);

                    return new()
                    {
                        Body = request.Body,
                    };
                }
                catch (HttpOperationException ex)
                {
                    throw new InvalidOperationException($"operation failed with status {ex.Response.StatusCode} and content: \n{ex.Response.Content}");
                }
            }
            else
            {
                if (@namespace is string)
                {
                    throw new InvalidOperationException("a namespace cannot be specified for a cluster-scoped resource");
                }

                try
                {
                    var response = await client.DeleteClusterCustomObjectWithHttpMessagesAsync(
                        api.Group ?? "",
                        api.Version,
                        api.Name,
                        name,
                        cancellationToken: cancellationToken);

                    return new()
                    {
                        Body = request.Body,
                    };
                }
                catch (HttpOperationException ex)
                {
                    throw new InvalidOperationException($"operation failed with status {ex.Response.StatusCode} and content: \n{ex.Response.Content}");
                }
            }
        }

        public async Task<GetResponse> Get(GetRequest request, CancellationToken cancellationToken)
        {
            var resource = request?.Body ?? throw new ArgumentNullException($"Request body not provided");

            var (name, @namespace) = GetKeys(resource);
            if (!GroupVersionKind.TryParse(resource.Type!, out var gvk))
            {
                throw new InvalidOperationException($"type: {resource.Type} is not a valid kubernetes type.");
            }

            var (config, clientConfig) = InitializeConfig(resource.Import!);

            // HEY LISTEN: We might want a way to reuse resources across operations. This will open a
            // connection to the Kubernetes cluster PER-operation. 
            using var client = new CoolKubernetesClient(clientConfig);
            var api = await GetApiResourceAsync(client, gvk.Value, cancellationToken);

            if (api.Namespaced)
            {
                try
                {
                    var response = await client.GetNamespacedCustomObjectWithHttpMessagesAsync(
                        api.Group ?? "",
                        api.Version,
                        @namespace ?? config.Namespace,
                        api.Name,
                        name,
                        cancellationToken: cancellationToken);

                    return new()
                    {
                        Body = new()
                        {
                            Type = resource.Type,

                            // HEY LISTEN: it's wierd to have to specify this on the return value.
                            Import = resource.Import,
                            Properties = JsonSerializer.SerializeToNode(response.Body),
                        }
                    };
                }
                catch (HttpOperationException ex)
                {
                    throw new InvalidOperationException($"operation failed with status {ex.Response.StatusCode} and content: \n{ex.Response.Content}");
                }
            }
            else
            {
                if (@namespace is string)
                {
                    throw new InvalidOperationException("a namespace cannot be specified for a cluster-scoped resource");
                }

                try
                {
                    var response = await client.GetClusterCustomObjectWithHttpMessagesAsync(
                        api.Group ?? "",
                        api.Version,
                        api.Name,
                        name,
                        cancellationToken: cancellationToken);

                    return new()
                    {
                        Body = new()
                        {
                            Type = resource.Type,

                            // HEY LISTEN: it's wierd to have to specify this on the return value.
                            Import = resource.Import,
                            Properties = JsonSerializer.SerializeToNode(response.Body),
                        }
                    };
                }
                catch (HttpOperationException ex)
                {
                    throw new InvalidOperationException($"operation failed with status {ex.Response.StatusCode} and content: \n{ex.Response.Content}");
                }
            }
        }

        public async Task<PreviewSaveResponse> PreviewSave(PreviewSaveRequest request, CancellationToken cancellationToken)
        {
            var resource = request?.Body ?? throw new ArgumentNullException($"Request body not provided");

            var (name, @namespace) = GetKeys(resource);
            if (!GroupVersionKind.TryParse(resource.Type!, out var gvk))
            {
                throw new InvalidOperationException($"type: {resource.Type} is not a valid kubernetes type.");
            }

            // User is not required to type these in, they are implicit as part of the type.
            resource.Properties!["apiVersion"] = JsonValue.Create(gvk.Value.ApiVersion);
            resource.Properties!["kind"] = JsonValue.Create(gvk.Value.Kind);

            var (config, clientConfig) = InitializeConfig(resource.Import!);

            // HEY LISTEN: We might want a way to reuse resources across operations. This will open a
            // connection to the Kubernetes cluster PER-operation. 
            using var client = new CoolKubernetesClient(clientConfig);
            var api = await GetApiResourceAsync(client, gvk.Value, cancellationToken);

            if (api.Namespaced)
            {
                try
                {
                    var response = await client.PatchNamespacedCustomObjectWithHttpMessagesAsync(
                        new k8s.Models.V1Patch(resource.Properties, k8s.Models.V1Patch.PatchType.ApplyPatch),
                        api.Group ?? "",
                        api.Version,
                        @namespace ?? config.Namespace,
                        api.Name,
                        name,
                        fieldManager: "bicep",
                        cancellationToken: cancellationToken);

                    return new()
                    {
                        Body = new()
                        {
                            Type = resource.Type,

                            // HEY LISTEN: it's wierd to have to specify this on the return value.
                            Import = resource.Import,
                            Properties = JsonSerializer.SerializeToNode(response.Body),
                        }
                    };
                }
                catch (HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.Created)
                {
                    // This is a bug in the Kubernetes SDK - https://github.com/kubernetes-client/csharp/issues/576#issuecomment-796868678
                    return new()
                    {
                        Body = new()
                        {
                            Type = resource.Type,

                            // HEY LISTEN: it's wierd to have to specify this on the return value.
                            Import = resource.Import,
                            Properties = JsonObject.Parse(ex.Response.Content),
                        }
                    };
                }
                catch (HttpOperationException ex)
                {
                    throw new InvalidOperationException($"operation failed with status {ex.Response.StatusCode} and content: \n{ex.Response.Content}");
                }
            }
            else
            {
                if (@namespace is string)
                {
                    throw new InvalidOperationException("a namespace cannot be specified for a cluster-scoped resource");
                }

                try
                {
                    var response = await client.PatchClusterCustomObjectWithHttpMessagesAsync(
                        new k8s.Models.V1Patch(resource.Properties, k8s.Models.V1Patch.PatchType.ApplyPatch),
                        api.Group ?? "",
                        api.Version,
                        api.Name,
                        name,
                        fieldManager: "bicep",
                        cancellationToken: cancellationToken);

                    return new()
                    {
                        Body = new()
                        {
                            Type = resource.Type,

                            // HEY LISTEN: it's wierd to have to specify this on the return value.
                            Import = resource.Import,
                            Properties = JsonSerializer.SerializeToNode(response.Body),
                        }
                    };
                }
                catch (HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.Created)
                {
                    // This is a bug in the Kubernetes SDK - https://github.com/kubernetes-client/csharp/issues/576#issuecomment-796868678
                    return new()
                    {
                        Body = new()
                        {
                            Type = resource.Type,

                            // HEY LISTEN: it's wierd to have to specify this on the return value.
                            Import = resource.Import,
                            Properties = JsonObject.Parse(ex.Response.Content),
                        }
                    };
                }
                catch (HttpOperationException ex)
                {
                    throw new InvalidOperationException($"operation failed with status {ex.Response.StatusCode} and content: \n{ex.Response.Content}");
                }
            }
        }

        public async Task<SaveResponse> Save(SaveRequest request, CancellationToken cancellationToken)
        {
            var resource = request?.Body ?? throw new ArgumentNullException($"Request body not provided");

            var (name, @namespace) = GetKeys(resource);
            if (!GroupVersionKind.TryParse(resource.Type!, out var gvk))
            {
                throw new InvalidOperationException($"type: {resource.Type} is not a valid kubernetes type.");
            }

            // User is not required to type these in, they are implicit as part of the type.
            resource.Properties!["apiVersion"] = JsonValue.Create(gvk.Value.ApiVersion);
            resource.Properties!["kind"] = JsonValue.Create(gvk.Value.Kind);

            var (config, clientConfig) = InitializeConfig(resource.Import!);

            // HEY LISTEN: We might want a way to reuse resources across operations. This will open a
            // connection to the Kubernetes cluster PER-operation. 
            using var client = new CoolKubernetesClient(clientConfig);
            var api = await GetApiResourceAsync(client, gvk.Value, cancellationToken);

            if (api.Namespaced)
            {
                try
                {
                    var response = await client.PatchNamespacedCustomObjectWithHttpMessagesAsync(
                        new k8s.Models.V1Patch(resource.Properties, k8s.Models.V1Patch.PatchType.ApplyPatch),
                        api.Group ?? "",
                        api.Version,
                        @namespace ?? config.Namespace,
                        api.Name,
                        name,
                        fieldManager: "bicep",
                        cancellationToken: cancellationToken);

                    return new()
                    {
                        Body = new()
                        {
                            Type = resource.Type,

                            // HEY LISTEN: it's wierd to have to specify this on the return value.
                            Import = resource.Import,
                            Properties = JsonSerializer.SerializeToNode(response.Body),
                        }
                    };
                }
                catch (HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.Created)
                {
                    // This is a bug in the Kubernetes SDK - https://github.com/kubernetes-client/csharp/issues/576#issuecomment-796868678
                    return new()
                    {
                        Body = new()
                        {
                            Type = resource.Type,

                            // HEY LISTEN: it's wierd to have to specify this on the return value.
                            Import = resource.Import,
                            Properties = JsonObject.Parse(ex.Response.Content),
                        }
                    };
                }
                catch (HttpOperationException ex)
                {
                    // TODO: return error contract
                    throw new InvalidOperationException($"operation failed with status {ex.Response.StatusCode} and content: \n{ex.Response.Content}");
                }
            }
            else
            {
                if (@namespace is string)
                {
                    throw new InvalidOperationException("a namespace cannot be specified for a cluster-scoped resource");
                }

                try
                {
                    var response = await client.PatchClusterCustomObjectWithHttpMessagesAsync(
                        new k8s.Models.V1Patch(resource.Properties, k8s.Models.V1Patch.PatchType.ApplyPatch),
                        api.Group ?? "",
                        api.Version,
                        api.Name,
                        name,
                        fieldManager: "bicep",
                        cancellationToken: cancellationToken);

                    return new()
                    {
                        Body = new()
                        {
                            Type = resource.Type,

                            // HEY LISTEN: it's wierd to have to specify this on the return value.
                            Import = resource.Import,
                            Properties = JsonSerializer.SerializeToNode(response.Body),
                        }
                    };
                }
                catch (HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.Created)
                {
                    // This is a bug in the Kubernetes SDK - https://github.com/kubernetes-client/csharp/issues/576#issuecomment-796868678
                    return new()
                    {
                        Body = new()
                        {
                            Type = resource.Type,

                            // HEY LISTEN: it's wierd to have to specify this on the return value.
                            Import = resource.Import,
                            Properties = JsonObject.Parse(ex.Response.Content),
                        }
                    };
                }
                catch (HttpOperationException ex)
                {
                    // TODO: return error contract
                    throw new InvalidOperationException($"operation failed with status {ex.Response.StatusCode} and content: \n{ex.Response.Content}");
                }
            }
        }

        private (KubernetesConfig config, KubernetesClientConfiguration clientConfig) InitializeConfig(ExtensibleImport import)
        {
            var config = import.Config?.GetValue<KubernetesConfig>() ?? new KubernetesConfig();
            if (config.KubeConfig.Length > 0)
            {

                return (config, KubernetesClientConfiguration.BuildConfigFromConfigFile(new MemoryStream(config.KubeConfig), currentContext: config.Context));
            }
            else
            {
                return (config, KubernetesClientConfiguration.BuildDefaultConfig());
            }
        }

        // HEY LISTEN: It would be nice to be able to cache this information across different operations in the same deployment.
        // The set of resources is different per-cluster, and relatively big to query. You'd also need to handle the case where
        // and operation changed the set of resources while in progress (requery on cache miss would likely work).
        private async Task<k8s.Models.V1APIResource> GetApiResourceAsync(CoolKubernetesClient client, GroupVersionKind gvk, CancellationToken cancellationToken)
        {
            var resources = await client.GetAPIResourcesAsync(gvk.Group, gvk.Version, cancellationToken);
            foreach (var resource in resources.Resources)
            {
                if (resource.Kind == gvk.Kind)
                {
                    // Not set by server for some reason :-/
                    resource.Group = gvk.Group;
                    resource.Version = gvk.Version;

                    return resource;
                }
            }

            throw new InvalidOperationException($"API Resource: {gvk} is not supported by the cluster.");
        }

        // HEY LISTEN: Why is JsonObject featured so prominently in the API? It's bad mojo to surface a 3rd party type
        // as an exchange type. I'd suggest using byte[] or stream or string.
        private static (string name, string? @namespace) GetKeys(ExtensibleResourceBody resource)
        {
            if ((resource.Properties as JsonObject)!.TryGetPropertyValue("metadata", out var token) && token is JsonObject metadata)
            {
                metadata.TryGetPropertyValue("name", out var nameToken);
                metadata.TryGetPropertyValue("namespace", out var namespaceToken);

                if (nameToken is not JsonValue)
                {
                    throw new InvalidOperationException("resource does not contain the required string property '.metadata.name'");
                }

                // Namespace must be a string if it is specified.
                if (namespaceToken is not JsonValue)
                {
                    throw new InvalidOperationException("property '.metadata.namespace' must be a string");
                }

                return (nameToken.GetValue<string>(), namespaceToken?.GetValue<string>());
            }

            throw new InvalidOperationException("resource does not contain the required object property '.metadata'");
        }
    }
}
