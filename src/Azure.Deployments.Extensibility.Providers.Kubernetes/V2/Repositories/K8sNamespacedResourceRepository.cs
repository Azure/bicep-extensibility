// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Models;
using Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Utils;
using k8s;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Repositories
{
    public class K8sNamespacedResourceRepository(IKubernetes kubernetes, string @namespace) : IK8sResourceRepository
    {
        public async Task<K8sResource> SaveAsync(K8sResource resource, bool dryRun, CancellationToken cancellationToken)
        {
            var properties = await kubernetes.CustomObjects.PatchNamespacedCustomObjectAsync(
                    resource.ToV1Patch(),
                    resource.Group,
                    resource.Version,
                    @namespace,
                    resource.ReferenceId.Plural,
                    resource.Name,
                    fieldManager: "AzureDeployments",
                    force: true,
                    dryRun: dryRun ? "All" : null,
                    cancellationToken: cancellationToken);

            return resource with { Properties = SerializationHelper.SerializeToJsonObject(properties) };
        }

        public async Task<K8sResource?> TryGetByReferenceIdAsync(K8sResourceReferenceId referenceId, CancellationToken cancellationToken)
        {
            var resourceType = new K8sResourceType(referenceId.Group, referenceId.Version, referenceId.Kind);
            var properties = await kubernetes.CustomObjects.GetNamespacedCustomObjectAsync(
                    referenceId.Group,
                    referenceId.Version,
                    @namespace,
                    referenceId.Plural,
                    referenceId.Name,
                    cancellationToken: cancellationToken);

            return new K8sResource(referenceId, resourceType, SerializationHelper.SerializeToJsonObject(properties));
        }

        public Task DeleteByReferenceIdAsync(K8sResourceReferenceId referenceId, CancellationToken cancellationToken) =>
            kubernetes.CustomObjects.DeleteNamespacedCustomObjectAsync(
                referenceId.Group,
                referenceId.Version,
                @namespace,
                referenceId.Plural,
                referenceId.Name,
                cancellationToken: cancellationToken);
    }
}
