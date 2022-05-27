using Azure.Deployments.Extensibility.Core;
using Azure.Deployments.Extensibility.Core.Exceptions;
using Azure.Deployments.Extensibility.Core.Extensions;
using Azure.Deployments.Extensibility.Providers.Kubernetes.Models;
using k8s;
using k8s.Models;
using System.Text.Json;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.Extensions
{
    public static class IKubernetesExtensions
    {
        public static async Task<V1APIResource> FindApiAsync(this IKubernetes kubernetes, KubernetesResourceType resourceType, ExtensibleResource<KubernetesResourceProperties> resource, CancellationToken cancellationToken)
        {
            var client = new GenericClient(kubernetes, resourceType.Group, resourceType.Version, plural: "");

            var apiResouceList = await client.ListAsync<V1APIResourceList>(cancellationToken);
            var apiResource = apiResouceList.Resources.SingleOrDefault(x => x.Kind.Equals(resourceType.Kind, StringComparison.Ordinal));

            if (apiResource is null)
            {
                throw new ExtensibilityException(
                    "UnknownResourceKind",
                    resource.GetJsonPointer(x => x.Type),
                    @$"Unknown resource kind ""{resourceType.Kind}"" in resource type ""{resource.Type}"".");
            }

            return apiResource;
        }

        public static async Task<JsonElement> GetNamespacedResourceAsync(
            this IKubernetes kubernetes,
            KubernetesResourceType resourceType,
            ExtensibleResource<KubernetesResourceProperties> resource,
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
            KubernetesResourceType resourceType,
            ExtensibleResource<KubernetesResourceProperties> resource,
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
            KubernetesResourceType resourceType,
            ExtensibleResource<KubernetesResourceProperties> resource,
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
            KubernetesResourceType resourceType,
            ExtensibleResource<KubernetesResourceProperties> resource,
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
                    resource.GetJsonPointer(x => x.Properties.Metadata.Namespace!),
                    $"A namespace should not be specified for a cluster-scoped resource.");
            }

        }

        public static async Task<JsonElement> DeleteNamespacedResourceAsync(
            this IKubernetes kubernetes,
            KubernetesResourceType resourceType,
            ExtensibleResource<KubernetesResourceProperties> resource,
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
            KubernetesResourceType resourceType,
            ExtensibleResource<KubernetesResourceProperties> resource,
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
