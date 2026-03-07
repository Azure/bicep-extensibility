// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Json.Pointer;
using System.Collections.Immutable;

namespace Azure.Deployments.Extensibility.Core.V2.Contracts.Models;

/// <summary>
/// Contains metadata for a resource preview specification, indicating which properties
/// could not be evaluated at the time of the preview request.
/// </summary>
public record ResourcePreviewSpecificationMetadata
{
    /// <summary>
    /// Contains the JSON Pointers to the properties that could not be evaluated during the preview.
    /// The values of these properties are ARM template expressions (e.g., [reference(...)]).
    /// </summary>
    public required ImmutableArray<JsonPointer> Unevaluated { get; init; }
}
