// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Extensibility.AzureStorage;
using Extensibility.Kubernetes;
using Extensibility.Core.Contract;

namespace Extensibility.Standalone
{
    public static class Providers
    {
        private static readonly IReadOnlyDictionary<string, IExtensibilityProvider> ProvidersLookup = new Dictionary<string, IExtensibilityProvider>
        {
            ["AzureStorage"] = new AzureStorageProvider(),
            ["Kubernetes"] = new KubernetesProvider(),
        };

        public static IExtensibilityProvider GetProvider(string? name)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return ProvidersLookup.TryGetValue(name, out var provider) ? provider : throw new InvalidOperationException($"Failed to find provider \"{name}\"");
        }
    }
}
