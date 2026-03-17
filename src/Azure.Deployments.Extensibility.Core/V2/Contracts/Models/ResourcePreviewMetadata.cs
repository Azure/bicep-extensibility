// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Json.Pointer;
using System.Collections.Immutable;

namespace Azure.Deployments.Extensibility.Core.V2.Contracts.Models;

/// <summary>
/// Contains metadata about a resource preview, indicating which properties are read-only,
/// immutable, calculated, or unevaluated.
/// </summary>
public record ResourcePreviewMetadata
{
    /// <summary>
    /// Contains the JSON Pointers to the properties that are read-only.
    /// These properties are managed by the service and cannot be set by the client.
    /// </summary>
    public ImmutableArray<JsonPointer>? ReadOnly { get; init; }

    /// <summary>
    /// Contains the JSON Pointers to the properties that are immutable.
    /// These properties are not read-only, but they cannot be changed after the resource is created.
    /// They can be set during resource creation, but not updated later.
    /// </summary>
    public ImmutableArray<JsonPointer>? Immutable { get; init; }

    /// <summary>
    /// Contains the JSON Pointers to the properties that are unknown.
    /// </summary>
    public ImmutableArray<JsonPointer>? Unknown { get; init; }

    /// <summary>
    /// Contains the JSON Pointers to the properties that are calculated by the service.
    /// </summary>
    public ImmutableArray<JsonPointer>? Calculated { get; init; }

    /// <summary>
    /// Contains the JSON Pointers to the properties that could not be evaluated during the preview.
    /// </summary>
    public ImmutableArray<JsonPointer>? Unevaluated { get; init; }
}
