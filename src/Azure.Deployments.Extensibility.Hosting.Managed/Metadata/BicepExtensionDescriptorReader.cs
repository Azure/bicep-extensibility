// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Reflection;

namespace Azure.Deployments.Extensibility.Hosting.Managed.Metadata;

internal static class BicepExtensionDescriptorReader
{
    private const string NameKey = "Bicep.Extension.Name";
    private const string VersionKey = "Bicep.Extension.Version";

    internal static BicepExtensionDescriptor ReadEntryAssembly()
    {
        var entryAssembly = Assembly.GetEntryAssembly() ?? throw new InvalidOperationException(
            "The Bicep extension descriptor cannot be read because the entry assembly is unavailable.");

        return Read(entryAssembly);
    }

    internal static BicepExtensionDescriptor Read(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        return new BicepExtensionDescriptor(
            ReadValue(assembly, NameKey),
            ReadValue(assembly, VersionKey));
    }

    private static string ReadValue(Assembly assembly, string key)
    {
        var values = assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Where(attribute => string.Equals(attribute.Key, key, StringComparison.Ordinal))
            .ToArray();

        if (values.Length != 1 || values[0].Value is not { } value || string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Entry assembly metadata must contain exactly one non-empty '{key}' value.");
        }

        return value;
    }
}