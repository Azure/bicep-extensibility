// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Models;
using k8s.Models;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Services
{
    public interface IV1APIResourceCatalogService
    {
        Task<V1APIResource> FindV1APIResourceAsync(string providerVersion, K8sResourceType resourceType, CancellationToken cancellationToken);
    }
}
