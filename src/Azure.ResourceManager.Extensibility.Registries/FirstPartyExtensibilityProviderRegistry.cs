using Azure.ResourceManager.Extensibility.Core;
using Azure.ResourceManager.Extensibility.Providers.Kubernetes;

namespace Azure.ResourceManager.Extensibility.Registries
{
    public class FirstPartyExtensibilityProviderRegistry : IExtensibilityProviderRegistry
    {
        private static readonly IReadOnlyDictionary<string, IExtensibilityProvider> ProvidersByName = new Dictionary<string, IExtensibilityProvider>()
        {
            [KubernetesProvider.ProviderName] = new KubernetesProvider(),
        };

        public IExtensibilityProvider? TryGetExtensibilityProvider(string providerName) =>
            ProvidersByName.TryGetValue(providerName, out var provider) ? new FirstPartyExtensibilityProviderProxy(provider) : null;
    }
}