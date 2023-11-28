// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using k8s;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Repositories
{
    public sealed class K8sResourceRepositoryFactory : IK8sResourceRepositoryFactory
    {
        public IK8sResourceRepository CreateK8sResourceRepository(IKubernetes kubernetes, string? @namespace) => @namespace is not null
            ? new K8sNamespacedResourceRepository(kubernetes, @namespace)
            : new K8sClusterScopedResourceRepository(kubernetes);
    }
}
