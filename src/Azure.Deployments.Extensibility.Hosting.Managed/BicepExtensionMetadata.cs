// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Reflection;

namespace Azure.Deployments.Extensibility.Hosting.Managed;

/// <summary>
/// Represents the identity metadata of a Bicep extension.
/// </summary>
public sealed record BicepExtensionMetadata
{
    public const string ExtensionNameMetadataKey = "BicepExtensionName";
    public const string LegacyExtensionNameMetadataKey = "ExtensionName";
    public const string ExtensionVersionMetadataKey = "BicepExtensionVersion";
    public const string LegacyExtensionVersionMetadataKey = "ExtensionVersion";

    /// <summary>
    /// Initializes a new instance of the <see cref="BicepExtensionMetadata"/> class.
    /// </summary>
    /// <param name="name">The name of the extension.</param>
    /// <param name="version">The exact semantic version of the extension.</param>
    public BicepExtensionMetadata(string name, string version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);

        this.Name = name;
        this.Version = version;
    }

    /// <summary>
    /// Gets the name of the extension.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the exact semantic version of the extension.
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// Reads the extension identity metadata from the specified assembly's custom attributes.
    /// </summary>
    /// <param name="assembly">The assembly to read metadata from.</param>
    /// <returns>The resolved <see cref="BicepExtensionMetadata"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when extension identity metadata is missing or invalid.</exception>
    public static BicepExtensionMetadata FromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var attributes = assembly.GetCustomAttributes<AssemblyMetadataAttribute>().ToArray();

        var name = attributes.FirstOrDefault(a =>
            string.Equals(a.Key, ExtensionNameMetadataKey, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(a.Key, LegacyExtensionNameMetadataKey, StringComparison.OrdinalIgnoreCase))?.Value;

        var version = attributes.FirstOrDefault(a =>
            string.Equals(a.Key, ExtensionVersionMetadataKey, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(a.Key, LegacyExtensionVersionMetadataKey, StringComparison.OrdinalIgnoreCase))?.Value;

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(version))
        {
            throw new InvalidOperationException(
                $"Bicep extension identity metadata is missing from assembly '{assembly.FullName}'. " +
                $"Ensure that the project defines '{ExtensionNameMetadataKey}' and '{ExtensionVersionMetadataKey}' MSBuild properties, " +
                $"or that the assembly contains '{ExtensionNameMetadataKey}' and '{ExtensionVersionMetadataKey}' AssemblyMetadataAttribute entries.");
        }

        return new BicepExtensionMetadata(name, version);
    }
}
