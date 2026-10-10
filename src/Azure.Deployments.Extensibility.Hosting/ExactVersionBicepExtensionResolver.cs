// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore;

namespace Azure.Deployments.Extensibility.Hosting;

/// <summary>
/// Resolves a single exact extension version without parsing route versions as semantic ranges.
/// </summary>
public sealed class ExactVersionBicepExtensionResolver : IBicepExtensionResolver
{
    private readonly string configuredVersion;
    private readonly BicepExtensionRegistration registration;

    public ExactVersionBicepExtensionResolver(string configuredVersion, BicepExtensionRegistration registration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configuredVersion);
        ArgumentNullException.ThrowIfNull(registration);

        this.configuredVersion = configuredVersion;
        this.registration = registration;
    }

    public BicepExtensionRegistration? Resolve(string extensionVersion)
    {
        if (string.IsNullOrWhiteSpace(extensionVersion))
        {
            return null;
        }

        return string.Equals(extensionVersion, this.configuredVersion, StringComparison.Ordinal)
            ? this.registration
            : null;
    }
}
