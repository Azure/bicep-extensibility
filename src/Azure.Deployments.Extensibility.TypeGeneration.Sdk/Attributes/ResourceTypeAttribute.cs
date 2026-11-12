// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Deployments.Extensibility.TypeGeneration.Attributes;

/// <summary>
/// Marks a class as a Bicep resource type for build-time discovery.
/// </summary>
/// <remarks>
/// Use either the legacy constructor API version or <see cref="ApiVersions"/> to register versions.
/// The frontend interprets these inputs without instantiating the attribute.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ResourceTypeAttribute : Attribute
{
    public ResourceTypeAttribute(string name, string? apiVersion = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        ApiVersion = apiVersion;
    }

    public string Name { get; }

    public string? ApiVersion { get; }

    /// <summary>
    /// Gets or initializes the API versions that share the resource's current schema.
    /// </summary>
    public string[] ApiVersions { get; init; } = [];

    /// <summary>
    /// Gets the legacy identity based on <see cref="Name"/> and <see cref="ApiVersion"/>.
    /// This does not expand <see cref="ApiVersions"/>.
    /// </summary>
    public string FullName
        => ApiVersion is null ? Name : $"{Name}@{ApiVersion}";
}
