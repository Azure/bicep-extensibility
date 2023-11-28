// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Repositories;
using Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Extensions
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddKubernetesV2Provider(this IServiceCollection services)
        {
            services
                .AddSingleton<IK8sResourceRepositoryFactory, K8sResourceRepositoryFactory>()
                .AddSingleton<IV1APIResourceCatalogServiceFactory, V1APIResourceCatalogServiceFactory>()
                .AddSingleton<KubernetesProvider>();

            return services;
        }
    }
}
