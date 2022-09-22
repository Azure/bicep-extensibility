// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core;
using Azure.Deployments.Extensibility.Providers.Kubernetes;
using Azure.Deployments.Extensibility.Providers.ThirdParty;

namespace Azure.Deployments.Extensibility.DevHost.Registries
{
    public class FirstPartyExtensibilityProviderRegistry : IExtensibilityProviderRegistry
    {
        private readonly IThirdPartyExtensibilityProvider thirdPartyProvider;

        public FirstPartyExtensibilityProviderRegistry(IThirdPartyExtensibilityProvider thirdPartyProvider)
        {
            this.thirdPartyProvider = thirdPartyProvider;
        }

        private static readonly IReadOnlyDictionary<string, IExtensibilityProvider> ProvidersByName = new Dictionary<string, IExtensibilityProvider>()
        {
            [KubernetesProvider.ProviderName] = new KubernetesProvider(),
        };

        public IExtensibilityProvider? TryGetExtensibilityProvider(string providerName)
        {
            if (ProvidersByName.TryGetValue(providerName, out var provider))
            {
                return provider;
            }

            return thirdPartyProvider;
        }
    }
}
