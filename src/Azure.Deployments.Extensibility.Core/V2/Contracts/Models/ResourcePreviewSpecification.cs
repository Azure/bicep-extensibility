// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Core.V2.Contracts.Models;

/// <summary>
/// Represents the specification for a resource preview request.
/// Extends <see cref="ResourceSpecification{TProperties, TConfig}"/> with optional preview metadata
/// indicating which properties contain unevaluated ARM template expressions.
/// </summary>
/// <typeparam name="TProperties">The type representing the resource properties.</typeparam>
/// <typeparam name="TConfig">The type representing the extension configuration.</typeparam>
/// <remarks>
/// See <see href="https://github.com/Azure/bicep-extensibility/blob/main/docs/v2/preview-operation.md">preview-operation.md</see>.
/// </remarks>
public record ResourcePreviewSpecification<TProperties, TConfig> : ResourceSpecification<TProperties, TConfig>
{
    /// <summary>
    /// Optional metadata for the preview specification, such as properties that could not be evaluated.
    /// </summary>
    public ResourcePreviewSpecificationMetadata? Metadata { get; init; }
}

/// <inheritdoc cref="ResourcePreviewSpecification{TProperties, TConfig}"/>
public record ResourcePreviewSpecification<TProperties> : ResourcePreviewSpecification<TProperties, JsonObject>
{
}

/// <summary>
/// Represents a resource preview specification with untyped JSON properties and configuration.
/// </summary>
/// <inheritdoc cref="ResourcePreviewSpecification{TProperties, TConfig}"/>
public record ResourcePreviewSpecification : ResourcePreviewSpecification<JsonObject>
{
}
