// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using k8s;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Repositories
{
    public interface IK8sResourceRepositoryFactory
    {
        IK8sResourceRepository CreateK8sResourceRepository(IKubernetes kubernetes, string? @namespace);
    }
}
