// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore;

namespace Azure.Deployments.Extensibility.Hosting.Managed;

internal sealed class ManagedBicepExtensionResolver : IBicepExtensionResolver
{
    private readonly BicepExtensionIdentity identity;
    private readonly BicepExtensionRegistration registration;

    public ManagedBicepExtensionResolver(
        BicepExtensionIdentity identity,
        BicepExtensionRegistration registration)
    {
        this.identity = identity;
        this.registration = registration;
    }

    public BicepExtensionRegistration? Resolve(string extensionVersion) =>
        string.Equals(extensionVersion, this.identity.Version, StringComparison.Ordinal)
            ? this.registration
            : null;
}
