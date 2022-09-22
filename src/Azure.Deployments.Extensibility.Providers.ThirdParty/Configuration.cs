// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Azure.Deployments.Extensibility.Providers.ThirdParty.ACI;
using Azure.Deployments.Extensibility.Providers.ThirdParty.AzureContext;

namespace Azure.Deployments.Extensibility.Providers.ThirdParty
{
    public static class ConfigurationExtensions
    {
        public static void ConfigureThirdPartyExtensibility(this IServiceCollection  services)
        {
            services.AddScoped<IAzureRequestContext, AzureRequestContext>();
            services.AddScoped<IAzureContainerInstanceHost, AzureContainerInstanceHost>();
            services.AddScoped<IThirdPartyExtensibilityProvider, ThirdPartyExtensibilityProvider>();
        }
    }
}
