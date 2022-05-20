using Azure.ResourceManager.Extensibility.Core;
using Azure.ResourceManager.Extensibility.Core.Extensions;
using Azure.ResourceManager.Extensibility.Providers.Kubernetes.Models;
using k8s;
using k8s.Models;
using System.Text.Json;

namespace Azure.ResourceManager.Extensibility.Providers.Kubernetes
{
    public class KubernetesProvider : IExtensibilityProvider
    {
        public const string ProviderName = "Kubernetes";

        public Task<ExtensibilityResponse> DeleteAsync(ExtensibilityRequest request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<ExtensibilityResponse> GetAsync(ExtensibilityRequest request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<ExtensibilityResponse> PreviewSaveAsync(ExtensibilityRequest request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<ExtensibilityResponse> SaveAsync(ExtensibilityRequest request, CancellationToken cancellationToken)
        {
            var (config, resourceType, resourceProperties) = ValidateRequest(request);

            var kubernetes = CreateKubernetes(config);
            var genericClient = new GenericClient(kubernetes, resourceType.Group, resourceType.Version, plural: "");

            var apiResouceList = await genericClient.ListAsync<V1APIResourceList>();
            var apiResource = apiResouceList.Resources.Single(x => x.Kind == resourceType.Kind);

            object patchResponse;
            var patchBody = new V1Patch(resourceProperties.ToJsonString(), V1Patch.PatchType.ApplyPatch);

            if (apiResource.Namespaced)
            {
                patchResponse = await kubernetes.CustomObjects.PatchNamespacedCustomObjectAsync(
                    patchBody,
                    resourceType.Group,
                    resourceType.Version,
                    resourceProperties.Metadata.Namespace ?? config.Namespace,
                    apiResource.Name,
                    resourceProperties.Metadata.Name,
                    fieldManager: "Bicep",
                    force: true,
                    cancellationToken: cancellationToken);
            }
            else
            {
                if (resourceProperties.Metadata.Namespace is not null)
                {
                    // TODO: Error response.
                    throw new InvalidOperationException("a namespace cannot be specified for a cluster-scoped resource");
                }

                patchResponse = await kubernetes.CustomObjects.PatchClusterCustomObjectAsync(
                    patchBody,
                    resourceType.Group,
                    resourceType.Version,
                    apiResource.Name,
                    resourceProperties.Metadata.Name,
                    fieldManager: "Bicep",
                    force: true,
                    cancellationToken: cancellationToken);
            }

            return new ExtensibilitySuccessResponse(request.Resource with
            {
                Properties = JsonSerializer.SerializeToElement(patchResponse),
            });
        }

        private static (KubernetesConfig, KubernetesResourceType, KubernetesResourceProperties) ValidateRequest(ExtensibilityRequest request)
        {
            var (import, resource) = request.Validate<KubernetesConfig, KubernetesResourceProperties>(
                KubernetesConfig.Schema,
                KubernetesResourceType.Regex,
                KubernetesResourceProperties.Schema);

            var resourceType = KubernetesResourceType.Parse(resource.Type);

            resource.Properties
                .PatchProperty("apiVersion", resourceType.ApiVersion)
                .PatchProperty("kind", resourceType.Kind);

            return (import.Config, resourceType, resource.Properties);
        }

        private static IKubernetes CreateKubernetes(KubernetesConfig config) => new k8s.Kubernetes(
            KubernetesClientConfiguration.BuildConfigFromConfigFile(
                new MemoryStream(config.KubeConfig),
                currentContext: config.Context));
    }
}