// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Deployments.Extensibility.Core.V2.Contracts.Models;

/// <summary>
/// Represents the specification for a resource preview request, extending <see cref="ResourceSpecification"/>
/// with optional preview metadata.
/// </summary>
public record ResourcePreviewSpecification : ResourceSpecification
{
    /// <summary>
    /// Optional metadata for the preview specification, such as properties that could not be evaluated.
    /// </summary>
    public ResourcePreviewSpecificationMetadata? Metadata { get; init; }
}
