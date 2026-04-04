// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Deployments.Extensibility.Core
{
    /// <summary>
    /// Registry for looking up V1 extensibility providers by name.
    /// </summary>
    public interface IExtensibilityProviderRegistry
    {
        /// <summary>
        /// Attempt to retrieve the extensibility provider registered under the specified name.
        /// </summary>
        /// <param name="providerName">The provider name to look up.</param>
        /// <returns>The provider if found; otherwise, <see langword="null"/>.</returns>
        IExtensibilityProvider? TryGetExtensibilityProvider(string providerName);
    }
}
