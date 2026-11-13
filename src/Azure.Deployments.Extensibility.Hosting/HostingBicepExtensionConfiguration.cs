// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Deployments.Extensibility.Hosting;

internal sealed class HostingBicepExtensionConfiguration
{
    public HostingBicepExtensionConfiguration(string extensionVersion, string extensionIdentity)
    {
        this.ExtensionVersion = extensionVersion;
        this.ExtensionIdentity = extensionIdentity;
    }

    public string ExtensionVersion { get; }

    public string ExtensionIdentity { get; }

    public bool AggregationConfigured { get; set; }
}
