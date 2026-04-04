// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Json.Pointer;
using System.Collections.Immutable;

namespace Azure.Deployments.Extensibility.Core.V2.Contracts.Models;

/// <summary>
/// Contains metadata about a resource preview response, classifying properties by their
/// mutability and evaluability characteristics for use by the deployment engine in
/// What-If and preflight validation.
/// </summary>
/// <remarks>
/// See <see href="https://github.com/Azure/bicep-extensibility/blob/main/docs/v2/preview-operation.md#preview-metadata">preview-operation.md</see>.
/// </remarks>
public record ResourcePreviewMetadata
{
    /// <summary>
    /// JSON Pointers to properties managed entirely by the service. The user cannot set these.
    /// </summary>
    public ImmutableArray<JsonPointer>? ReadOnly { get; init; }

    /// <summary>
    /// JSON Pointers to properties that can be set at creation but cannot be changed on subsequent updates.
    /// </summary>
    public ImmutableArray<JsonPointer>? Immutable { get; init; }

    /// <summary>
    /// JSON Pointers to properties whose values cannot be determined at preview time,
    /// typically because they depend on unevaluated expressions or unavailable external state.
    /// </summary>
    public ImmutableArray<JsonPointer>? Unknown { get; init; }

    /// <summary>
    /// JSON Pointers to properties whose values are computed at operation time and would
    /// differ if the operation were performed later.
    /// </summary>
    public ImmutableArray<JsonPointer>? Calculated { get; init; }

    /// <summary>
    /// JSON Pointers to properties containing ARM template language expressions that the
    /// deployment engine could not resolve. A path should only appear here when no more
    /// specific category (readOnly, immutable, calculated, unknown) applies.
    /// </summary>
    public ImmutableArray<JsonPointer>? Unevaluated { get; init; }
}
