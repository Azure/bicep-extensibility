// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore;

namespace Azure.Deployments.Extensibility.Hosting.Managed;

/// <summary>
/// An <see cref="IBicepExtensionResolver"/> that matches incoming extension versions ordinally against a single exact version.
/// </summary>
internal sealed class ExactVersionBicepExtensionResolver : IBicepExtensionResolver
{
    private readonly string extensionVersion;
    private readonly BicepExtensionRegistration registration;

    public ExactVersionBicepExtensionResolver(string extensionVersion, BicepExtensionRegistration registration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(extensionVersion);
        ArgumentNullException.ThrowIfNull(registration);

        this.extensionVersion = extensionVersion;
        this.registration = registration;
    }

    public string ExtensionVersion => this.extensionVersion;

    public BicepExtensionRegistration Registration => this.registration;

    public BicepExtensionRegistration? Resolve(string extensionVersion)
    {
        if (string.Equals(this.extensionVersion, extensionVersion, StringComparison.Ordinal))
        {
            return this.registration;
        }

        return null;
    }
}
