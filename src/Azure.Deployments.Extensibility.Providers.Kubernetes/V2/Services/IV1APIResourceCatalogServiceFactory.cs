// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using k8s;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Services
{
    public interface IV1APIResourceCatalogServiceFactory
    {
        IV1APIResourceCatalogService CreateV1APIResourceCatalogService(IKubernetes kubernetes);
    }
}
