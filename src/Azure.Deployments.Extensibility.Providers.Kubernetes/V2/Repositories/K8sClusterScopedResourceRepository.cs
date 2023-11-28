// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Constants;
using Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Models;
using Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Utils;
using k8s;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Repositories
{
    public class K8sClusterScopedResourceRepository(IKubernetes kubernetes) : IK8sResourceRepository
    {
        public async Task<K8sResource> SaveAsync(K8sResource resource, bool dryRun, CancellationToken cancellationToken)
        {
            var properties = await kubernetes.CustomObjects.PatchClusterCustomObjectAsync(
                    resource.ToV1Patch(),
                    resource.Group,
                    resource.Version,
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
            var properties = await kubernetes.CustomObjects.GetClusterCustomObjectAsync(
                    referenceId.Group,
                    referenceId.Version,
                    referenceId.Plural,
                    referenceId.Name,
                    cancellationToken: cancellationToken);

            return new K8sResource(referenceId, resourceType, SerializationHelper.SerializeToJsonObject(properties));
        }

        public Task DeleteByReferenceIdAsync(K8sResourceReferenceId referenceId, CancellationToken cancellationToken) =>
            kubernetes.CustomObjects.DeleteClusterCustomObjectAsync(
                referenceId.Group,
                referenceId.Version,
                referenceId.Plural,
                referenceId.Name,
                cancellationToken: cancellationToken);
    }
}
