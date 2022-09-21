// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core;
using Azure.Deployments.Extensibility.Providers.Kubernetes;

namespace Azure.Deployments.Extensibility.DevHost.Registries
{
    public class FirstPartyExtensibilityProviderRegistry : IExtensibilityProviderRegistry
    {
        private static readonly IReadOnlyDictionary<string, IExtensibilityProvider> ProvidersByName = new Dictionary<string, IExtensibilityProvider>()
        {
            [KubernetesProvider.ProviderName] = new KubernetesProvider(),
        };

        private static readonly IReadOnlyDictionary<string, ExtensibilityProviderContainerRegistry> ProvidersByContainerRegistry = new Dictionary<string, ExtensibilityProviderContainerRegistry>(comparer: StringComparer.OrdinalIgnoreCase)
        {
            ["github"] = new ExtensibilityProviderContainerRegistry(ContainerRegistry: "bicepprovidersregistry.azurecr.io/github/server", ExternalPort: 8080),
        };

        public IExtensibilityProvider? TryGetExtensibilityProvider(string providerName) =>
            ProvidersByName.TryGetValue(providerName, out var provider) ? provider : null;

        public ExtensibilityProviderContainerRegistry? TryGetExtensibilityProviderContainerRegistry(string providerName) =>
            ProvidersByContainerRegistry.TryGetValue(providerName, out var providerContainerRegistry) ? providerContainerRegistry : null;
    }
}
