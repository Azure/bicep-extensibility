// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Reflection;

namespace Azure.Deployments.Extensibility.Hosting.Managed;

internal static class ManagedExtensionIdentityReader
{
    private const string ExtensionNameKey = "BicepExtensionName";
    private const string ExtensionVersionKey = "BicepExtensionVersion";

    public static BicepExtensionIdentity Read(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var metadata = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Where(attribute => attribute.Value is not null)
            .ToDictionary(attribute => attribute.Key, attribute => attribute.Value!, StringComparer.Ordinal);

        return new BicepExtensionIdentity(
            GetRequiredMetadata(metadata, ExtensionNameKey),
            GetRequiredMetadata(metadata, ExtensionVersionKey));
    }

    private static string GetRequiredMetadata(IReadOnlyDictionary<string, string> metadata, string key)
    {
        if (!metadata.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"The entry assembly must define the '{key}' MSBuild property to use the managed Bicep extension host.");
        }

        return value;
    }
}
