// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore;
using Azure.Deployments.Extensibility.Extensions.Kubernetes.Client;
using Azure.Deployments.Extensibility.Extensions.Kubernetes.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Azure.Deployments.Extensibility.Extensions.Kubernetes.DependencyInjection
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddKubernetesExtensionDispatcher(this IServiceCollection services) => services
            .AddSingleton<IK8sResourceSpecificationValidator, K8sResourceSpecificationValidator>()
            .AddSingleton<IK8sResourceReferenceValidator, K8sResourceReferenceValidator>()
            .AddSingleton<IK8sClientFactory, K8sClientFactory>()
            .AddKeyedSingleton<IExtensionDispatcher, KubernetesExtensionDispatcher>(KubernetesExtension.ExtensionName);
    }
}
