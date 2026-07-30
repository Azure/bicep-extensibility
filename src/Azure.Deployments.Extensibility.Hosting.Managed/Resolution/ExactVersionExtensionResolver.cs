// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore;
using Azure.Deployments.Extensibility.AspNetCore.Handlers;

namespace Azure.Deployments.Extensibility.Hosting.Managed.Resolution;

internal sealed class ExactVersionExtensionResolver : IBicepExtensionResolver
{
    private readonly string extensionVersion;
    private readonly BicepExtensionRegistration registration;

    internal ExactVersionExtensionResolver(
        string extensionVersion,
        BicepExtensionRegistration registration)
    {
        this.extensionVersion = extensionVersion;
        this.registration = registration;
    }

    public BicepExtensionRegistration? Resolve(string extensionVersion) =>
        string.Equals(extensionVersion, this.extensionVersion, StringComparison.Ordinal)
            ? this.registration
            : null;
}
