// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using k8s;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Services
{
    public sealed class K8sApiDiscoveryServiceFactory : IK8sApiDiscoveryServiceFactory
    {
        public IK8sApiDiscoveryService CreateK8sApiDiscoveryService(IKubernetes kubernetes) => new K8sApiDiscoveryService(kubernetes);
    }
}
