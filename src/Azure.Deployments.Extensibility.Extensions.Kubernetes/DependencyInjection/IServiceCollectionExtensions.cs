// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore;
using Azure.Deployments.Extensibility.Core.V2.Models;
using Azure.Deployments.Extensibility.Core.V2.Validation;
using Azure.Deployments.Extensibility.Extensions.Kubernetes.Client;
using Azure.Deployments.Extensibility.Extensions.Kubernetes.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Azure.Deployments.Extensibility.Extensions.Kubernetes.DependencyInjection
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddKubernetesExtensionDispatcher(this IServiceCollection services) => services
            .AddSingleton<IModelValidator<ResourceSpecification>, ResourceSpecificationValidator>()
            .AddSingleton<IModelValidator<ResourceReference>, ResourceReferenceValidator>()
            .AddSingleton<IK8sClientFactory, K8sClientFactory>()
            .AddKeyedSingleton<IExtensionDispatcher, KubernetesExtensionDispatcher>(KubernetesExtension.ExtensionName);
    }
}
