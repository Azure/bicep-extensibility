// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Deployments.Extensibility.Hosting.Managed;

/// <summary>
/// Identifies the Bicep extension hosted by the current process.
/// </summary>
public sealed class BicepExtensionIdentity
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BicepExtensionIdentity"/> class.
    /// </summary>
    public BicepExtensionIdentity(string name, string version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);

        this.Name = name;
        this.Version = version;
    }

    /// <summary>
    /// Gets the extension name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the exact extension version accepted by this process.
    /// </summary>
    public string Version { get; }
}
