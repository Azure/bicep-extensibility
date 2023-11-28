// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using k8s;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Services
{
    public sealed class V1APIResourceCatalogServiceFactory : IV1APIResourceCatalogServiceFactory
    {
        public IV1APIResourceCatalogService CreateV1APIResourceCatalogService(IKubernetes kubernetes) =>
            new V1APIResourceCatalogService(kubernetes);
    }
}
