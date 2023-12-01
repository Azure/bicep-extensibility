// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Models;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Services
{
    public interface IK8sApiDiscoveryService
    {
        Task<K8sApiMetadata> FindK8sApiMetadataAsync(string providerVersion, K8sResourceType resourceType, CancellationToken cancellationToken);
    }
}
