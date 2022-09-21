// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Identity;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using static Microsoft.Azure.Management.Fluent.Azure;

namespace Azure.Deployments.Extensibility.DevHost.AzureContext
{
    public interface IAzureRequestContext
    {
        IAzure GetAzureRequestContext();
    }

    public class AzureRequestContext : IAzureRequestContext
    {
        // TODO: Create AppSettingsReader
        private IConfiguration Configuration { get; }

        private static string AzureAdConfigurationSection => "AzureAd";

        public AzureRequestContext(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        public IAzure GetAzureRequestContext()
        {
            // TODO: Support different environments (clouds)
            var factory = new AzureCredentialsFactory();
            var credentials = factory.FromServicePrincipal(
                        clientId: GetAppSettingValue("clientId"),
                        clientSecret: GetAppSettingValue("clientSecret"),
                        tenantId: GetAppSettingValue("tenantId"),
                        environment: AzureEnvironment.AzureGlobalCloud)
                    .WithDefaultSubscription(GetAppSettingValue("subscriptionId"));

            return Microsoft.Azure.Management.Fluent.Azure.Authenticate(azureCredentials: credentials)
                    .WithDefaultSubscription();
        }

        private string GetAppSettingValue(string key)
        {
            // TODO: Null check
            return Configuration.GetValue<string>($"{AzureAdConfigurationSection}:{key}");
        }
    }
}
