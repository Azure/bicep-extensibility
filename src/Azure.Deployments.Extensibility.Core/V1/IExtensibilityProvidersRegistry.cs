// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Deployments.Extensibility.Core
{
    public interface IExtensibilityProviderRegistry
    {
        IExtensibilityProvider? TryGetExtensibilityProvider(string providerName);
    }
}
