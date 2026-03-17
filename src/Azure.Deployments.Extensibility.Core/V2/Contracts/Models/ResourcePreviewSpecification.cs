// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Core.V2.Contracts.Models;

/// <summary>
/// Represents the specification for a resource preview request, extending <see cref="ResourceSpecification"/>
/// with optional preview metadata.
/// </summary>
public record ResourcePreviewSpecification<TProperties, TConfig> : ResourceSpecification<TProperties, TConfig>
{
    /// <summary>
    /// Optional metadata for the preview specification, such as properties that could not be evaluated.
    /// </summary>
    public ResourcePreviewSpecificationMetadata? Metadata { get; init; }
}

public record ResourcePreviewSpecification<TProperties> : ResourcePreviewSpecification<TProperties, JsonObject>
{
}

public record ResourcePreviewSpecification : ResourcePreviewSpecification<JsonObject>
{
}
