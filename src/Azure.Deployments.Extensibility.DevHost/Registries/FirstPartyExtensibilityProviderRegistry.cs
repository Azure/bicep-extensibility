// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core;
using Azure.Deployments.Extensibility.Providers.Kubernetes;
using Azure.Deployments.Extensibility.Providers.Graph;

namespace Azure.Deployments.Extensibility.DevHost.Registries
{
    public class FirstPartyExtensibilityProviderRegistry : IExtensibilityProviderRegistry
    {
        private static readonly IReadOnlyDictionary<string, IExtensibilityProvider> ProvidersByName = new Dictionary<string, IExtensibilityProvider>()
        {
            [KubernetesProvider.ProviderName] = new KubernetesProvider(),
            [GraphProvider.ProviderName] = new GraphProvider(),
        };

        public IExtensibilityProvider? TryGetExtensibilityProvider(string providerName) =>
            ProvidersByName.TryGetValue(providerName, out var provider) ? provider : null;
    }
}
