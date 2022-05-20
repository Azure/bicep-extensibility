using Azure.ResourceManager.Extensibility.Core;
using Azure.ResourceManager.Extensibility.Core.Extensions;
using Azure.ResourceManager.Extensibility.Providers.Kubernetes.Extensions;
using Azure.ResourceManager.Extensibility.Providers.Kubernetes.Models;
using k8s;
using k8s.Autorest;
using System.Net;
using System.Text.Json;

namespace Azure.ResourceManager.Extensibility.Providers.Kubernetes
{
    public class KubernetesProvider : IExtensibilityProvider
    {
        public const string ProviderName = "Kubernetes";

        public async Task<ExtensibilityResponse> DeleteAsync(ExtensibilityRequest request, CancellationToken cancellationToken)
        {
            var (config, resourceType, resource) = Validate(request);
            var @namespace = resource.Properties.Metadata.Namespace ?? config.Namespace;

            var kubernetes = CreateKubernetes(config);
            var api = await kubernetes.FindApiResourceByTypeAsync(resourceType, cancellationToken);

            await HandleHttpOperationExceptionAsync(async () => api.Namespaced
                ? await kubernetes.DeleteNamespacedResourceAsync(resource, resourceType, @namespace, api.Name, cancellationToken)
                : await kubernetes.DeleteClusterResourceAsync(resource, resourceType, api.Name, cancellationToken));

            return new ExtensibilitySuccessResponse(request.Resource);
        }

        public async Task<ExtensibilityResponse> GetAsync(ExtensibilityRequest request, CancellationToken cancellationToken)
        {
            var (config, resourceType, resource) = Validate(request);
            var @namespace = resource.Properties.Metadata.Namespace ?? config.Namespace;

            var kubernetes = CreateKubernetes(config);
            var api = await kubernetes.FindApiResourceByTypeAsync(resourceType, cancellationToken);

            var properties = await HandleHttpOperationExceptionAsync(async () => api.Namespaced
                ? await kubernetes.GetNamespacedResourceAsync(resource, resourceType, @namespace, api.Name, cancellationToken)
                : await kubernetes.GetClusterResourceAsync(resource, resourceType, api.Name, cancellationToken));

            return new ExtensibilitySuccessResponse(request.Resource with { Properties = properties });
        }

        public async Task<ExtensibilityResponse> PreviewSaveAsync(ExtensibilityRequest request, CancellationToken cancellationToken)
        {
            var (config, resourceType, resource) = Validate(request);
            var @namespace = resource.Properties.Metadata.Namespace ?? config.Namespace;

            var kubernetes = CreateKubernetes(config);
            var api = await kubernetes.FindApiResourceByTypeAsync(resourceType, cancellationToken);

            if (api.Namespaced)
            {
                try
                {
                    await kubernetes.CoreV1.ReadNamespaceAsync(@namespace, cancellationToken: cancellationToken);
                }
                catch (HttpOperationException notFoundException) when (notFoundException.Response.StatusCode == HttpStatusCode.NotFound)
                {
                    // For namespaced resources we have to handle a special case where the namespace is being created as part of the same
                    // template. When we do the dry-run request this will fail if the namespace does not yet exist. This isn't useful to us
                    // because if the namespace is being created as part of the template this would be a false positive. So for these cases
                    // we want to fall back to a "client" dry-run if the namespace has yet to be created. This is lower fidelity but it won't 
                    // block things that would work.
                    var metadata = resource.Properties.Metadata with { Namespace = @namespace };
                    var properties = resource.Properties with { Metadata = metadata };
                    var propertiesElement = JsonSerializer.SerializeToElement(properties, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    });

                    return new ExtensibilitySuccessResponse(request.Resource with { Properties = propertiesElement });
                }
                catch (HttpOperationException exception)
                {
                    throw exception.ToExtensibilityException();
                }
            }

            var dryRunProperties = await HandleHttpOperationExceptionAsync(async () => api.Namespaced
                ? await kubernetes.PatchNamespacedResourceAsync(resource, resourceType, @namespace, api.Name, cancellationToken, dryRun: "All")
                : await kubernetes.PatchClusterResourceAsync(resource, resourceType, api.Name, cancellationToken, dryRun: "All"));

            return new ExtensibilitySuccessResponse(request.Resource with { Properties = dryRunProperties });
        }

        public async Task<ExtensibilityResponse> SaveAsync(ExtensibilityRequest request, CancellationToken cancellationToken)
        {
            var (config, resourceType, resource) = Validate(request);
            var @namespace = resource.Properties.Metadata.Namespace ?? config.Namespace;

            var kubernetes = CreateKubernetes(config);
            var api = await kubernetes.FindApiResourceByTypeAsync(resourceType, cancellationToken);

            var patchedProperties = await HandleHttpOperationExceptionAsync(async () => api.Namespaced
                ? await kubernetes.PatchNamespacedResourceAsync(resource, resourceType, @namespace, api.Name, cancellationToken)
                : await kubernetes.PatchClusterResourceAsync(resource, resourceType, api.Name, cancellationToken));

            return new ExtensibilitySuccessResponse(request.Resource with { Properties = patchedProperties });
        }

        private static (KubernetesConfig, KubernetesResourceType, ExtensibleResource<KubernetesResourceProperties>) Validate(ExtensibilityRequest request)
        {
            var (import, resource) = request.Validate<KubernetesConfig, KubernetesResourceProperties>(
                KubernetesConfig.Schema,
                KubernetesResourceType.Regex,
                KubernetesResourceProperties.Schema);

            var resourceType = KubernetesResourceType.Parse(resource.Type);

            resource.Properties
                .PatchProperty("apiVersion", resourceType.ApiVersion)
                .PatchProperty("kind", resourceType.Kind);

            return (import.Config, resourceType, resource);
        }

        private static IKubernetes CreateKubernetes(KubernetesConfig config) => new k8s.Kubernetes(
            KubernetesClientConfiguration.BuildConfigFromConfigFile(
                new MemoryStream(config.KubeConfig),
                currentContext: config.Context));

        private async static Task<T> HandleHttpOperationExceptionAsync<T>(Func<Task<T>> asyncOperation)
        {
            try
            {
                return await asyncOperation();
            }
            catch (HttpOperationException exception)
            {
                throw exception.ToExtensibilityException();
            }
        }
    }
}