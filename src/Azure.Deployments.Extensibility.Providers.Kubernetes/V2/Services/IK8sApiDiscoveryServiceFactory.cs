// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using k8s;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Services
{
    public interface IK8sApiDiscoveryServiceFactory
    {
        IK8sApiDiscoveryService CreateK8sApiDiscoveryService(IKubernetes kubernetes);
    }
}
