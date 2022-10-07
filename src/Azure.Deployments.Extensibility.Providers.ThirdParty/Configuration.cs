// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Identity;
using Azure.ResourceManager;
using Microsoft.Extensions.DependencyInjection;

namespace Azure.Deployments.Extensibility.Providers.ThirdParty;

public static class ConfigurationExtensions
{
    public static void ConfigureThirdPartyExtensibility(this IServiceCollection  services)
    {
        services.AddScoped<IAppSettings, AppSettings>();
        services.AddScoped<IContainerManager, ContainerManager>();
        services.AddScoped<IThirdPartyExtensibilityProvider, ThirdPartyExtensibilityProvider>();
        services.AddSingleton<ArmClient>(s => new ArmClient(new DefaultAzureCredential()));
    }
}
