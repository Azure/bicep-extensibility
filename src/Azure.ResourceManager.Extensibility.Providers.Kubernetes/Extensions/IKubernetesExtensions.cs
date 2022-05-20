using Azure.ResourceManager.Extensibility.Core;
using Azure.ResourceManager.Extensibility.Core.Exceptions;
using Azure.ResourceManager.Extensibility.Core.Extensions;
using Azure.ResourceManager.Extensibility.Providers.Kubernetes.Models;
using Json.Pointer;
using k8s;
using k8s.Models;
using System.Text.Json;

namespace Azure.ResourceManager.Extensibility.Providers.Kubernetes.Extensions
{
    public static class IKubernetesExtensions
    {
        public static async Task<V1APIResource> FindApiResourceByTypeAsync(this IKubernetes kubernetes, KubernetesResourceType resourceType, CancellationToken cancellationToken)
        {
            var client = new GenericClient(kubernetes, resourceType.Group, resourceType.Version, plural: "");

            var apiResouceList = await client.ListAsync<V1APIResourceList>(cancellationToken);
            var apiResource = apiResouceList.Resources.SingleOrDefault(x => x.Kind.Equals(resourceType.Kind, StringComparison.Ordinal));

            if (apiResource is null)
            {
                throw new ExtensibilityException(
                    "UnknownResourceKind",
                    JsonPointer.Create<ExtensibleResource<KubernetesResourceProperties>>(x => x.Type).CamelCase(),
                    $"Unknown resource kind \"{resourceType.Kind}\".");
            }

            return apiResource;
        }

        public static async Task<JsonElement> GetNamespacedResourceAsync(
            this IKubernetes kubernetes,
            ExtensibleResource<KubernetesResourceProperties> resource,
            KubernetesResourceType resourceType,
            string @namespace,
            string plural,
            CancellationToken cancellationToken)
        {
            var result = await kubernetes.CustomObjects.GetNamespacedCustomObjectAsync(
                    resourceType.Group,
                    resourceType.Version,
                    @namespace,
                    plural,
                    resource.Properties.Metadata.Name,
                    cancellationToken);

            return JsonSerializer.SerializeToElement(result);
        }

        public static async Task<JsonElement> GetClusterResourceAsync(
            this IKubernetes kubernetes,
            ExtensibleResource<KubernetesResourceProperties> resource,
            KubernetesResourceType resourceType,
            string plural,
            CancellationToken cancellationToken)
        {
            EnsureNamespaceNotSpecifiedForClusterResource(resource);

            var result = await kubernetes.CustomObjects.GetClusterCustomObjectAsync(
                    resourceType.Group,
                    resourceType.Version,
                    plural,
                    resource.Properties.Metadata.Name,
                    cancellationToken);

            return JsonSerializer.SerializeToElement(result);
        }

        public static async Task<JsonElement> PatchNamespacedResourceAsync(
            this IKubernetes kubernetes,
            ExtensibleResource<KubernetesResourceProperties> resource,
            KubernetesResourceType resourceType,
            string @namespace,
            string plural,
            CancellationToken cancellationToken,
            string? dryRun = null)
        {
            var patched = await kubernetes.CustomObjects.PatchNamespacedCustomObjectAsync(
                    resource.Properties.ToV1Patch(),
                    resourceType.Group,
                    resourceType.Version,
                    @namespace,
                    plural,
                    resource.Properties.Metadata.Name,
                    fieldManager: "Bicep",
                    force: true,
                    dryRun: dryRun,
                    cancellationToken: cancellationToken);

            return JsonSerializer.SerializeToElement(patched);
        }


        public static async Task<JsonElement> PatchClusterResourceAsync(
            this IKubernetes kubernetes,
            ExtensibleResource<KubernetesResourceProperties> resource,
            KubernetesResourceType resourceType,
            string plural,
            CancellationToken cancellationToken,
            string? dryRun = null)
        {
            EnsureNamespaceNotSpecifiedForClusterResource(resource);

            var patched = await kubernetes.CustomObjects.PatchClusterCustomObjectAsync(
                    resource.Properties.ToV1Patch(),
                    resourceType.Group,
                    resourceType.Version,
                    plural,
                    resource.Properties.Metadata.Name,
                    fieldManager: "Bicep",
                    force: true,
                    dryRun: dryRun,
                    cancellationToken: cancellationToken);

            return JsonSerializer.SerializeToElement(patched);
        }

        private static void EnsureNamespaceNotSpecifiedForClusterResource(ExtensibleResource<KubernetesResourceProperties> resource)
        {
            if (resource.Properties.Metadata.Namespace is not null)
            {
                throw new ExtensibilityException(
                    "ShouldNotSpecifyNamespace",
                    JsonPointer.Create<ExtensibleResource<KubernetesResourceProperties>>(x => x.Properties.Metadata.Namespace!).CamelCase(),
                    $"A namespace should not be specified for a cluster-scoped resource.");
            }

        }

        public static async Task<JsonElement> DeleteNamespacedResourceAsync(
            this IKubernetes kubernetes,
            ExtensibleResource<KubernetesResourceProperties> resource,
            KubernetesResourceType resourceType,
            string @namespace,
            string plural,
            CancellationToken cancellationToken)
        {
            var result = await kubernetes.CustomObjects.DeleteNamespacedCustomObjectAsync(
                    resourceType.Group,
                    resourceType.Version,
                    @namespace,
                    plural,
                    resource.Properties.Metadata.Name,
                    cancellationToken: cancellationToken);

            return JsonSerializer.SerializeToElement(result);
        }

        public static async Task<JsonElement> DeleteClusterResourceAsync(
            this IKubernetes kubernetes,
            ExtensibleResource<KubernetesResourceProperties> resource,
            KubernetesResourceType resourceType,
            string plural,
            CancellationToken cancellationToken)
        {
            EnsureNamespaceNotSpecifiedForClusterResource(resource);

            var result = await kubernetes.CustomObjects.DeleteClusterCustomObjectAsync(
                    resourceType.Group,
                    resourceType.Version,
                    plural,
                    resource.Properties.Metadata.Name,
                    cancellationToken: cancellationToken);

            return JsonSerializer.SerializeToElement(result);
        }
    }
}
