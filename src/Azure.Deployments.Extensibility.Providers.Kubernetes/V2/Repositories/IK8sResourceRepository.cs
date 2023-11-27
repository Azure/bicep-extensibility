// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Models;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Repositories
{
    public interface IK8sResourceRepository
    {
        Task<K8sResource> SaveAsync(K8sResource resource, bool dryRun, CancellationToken cancellationToken);

        Task<K8sResource?> TryGetByReferenceIdAsync(K8sResourceReferenceId referenceId, CancellationToken cancellationToken);

        Task DeleteByReferenceIdAsync(K8sResourceReferenceId referenceId, CancellationToken cancellationToken);
    }
}
