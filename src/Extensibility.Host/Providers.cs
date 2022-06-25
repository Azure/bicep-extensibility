// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Extensibility.AzureStorage;
using Extensibility.Kubernetes;
using Extensibility.Core.Contract;

namespace Extensibility.Host
{
    public static class Providers
    {
        private static readonly IReadOnlyDictionary<string, IExtensibilityProvider> ProvidersLookup = new Dictionary<string, IExtensibilityProvider>
        {
            ["AzureStorage"] = new AzureStorageProvider(),
            ["Kubernetes"] = new KubernetesProvider(),
        };

        public static IExtensibilityProvider? TryGetProvider(string name)
            => ProvidersLookup.TryGetValue(name, out var provider) ? provider : null;
    }
}
