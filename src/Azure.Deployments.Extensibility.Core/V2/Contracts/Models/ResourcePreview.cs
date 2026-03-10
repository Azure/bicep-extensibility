// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Core.V2.Contracts.Models;

/// <summary>
/// Represents a preview of a resource, returned from the preview API.
/// </summary>
public record ResourcePreview<TProperties, TIdentifiers, TConfig>
{
    /// <summary>
    /// The type of the resource.
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// The API version of the resource.
    /// </summary>
    public string? ApiVersion { get; init; }

    /// <summary>
    /// The identifiers that uniquely identify the resource.
    /// </summary>
    public required TIdentifiers Identifiers { get; init; }

    /// <summary>
    /// The properties of the resource.
    /// </summary>
    public required TProperties Properties { get; init; }

    /// <summary>
    /// The status of the resource.
    /// </summary>
    public string? Status { get; init; }

    /// <summary>
    /// The configuration of the resource, with secrets removed.
    /// The property must be set if the resource has a configuration.
    /// </summary>
    public TConfig? Config { get; init; }

    /// <summary>
    /// The ID of the configuration for the resource.
    /// The property must be set if the resource has a configuration.
    /// </summary>
    public string? ConfigId { get; init; }

    /// <summary>
    /// The metadata describing which properties in the preview are read-only, immutable, calculated, or unevaluated.
    /// </summary>
    public ResourcePreviewMetadata? Metadata { get; init; }
}

public record ResourcePreview<TProperties, TIdentifiers> : ResourcePreview<TProperties, TIdentifiers, JsonObject>
{
}

public record ResourcePreview : ResourcePreview<JsonObject, JsonObject>
{
}
