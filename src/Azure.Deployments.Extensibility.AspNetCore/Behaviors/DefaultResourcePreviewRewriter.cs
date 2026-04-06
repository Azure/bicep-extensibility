// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using Json.Pointer;

namespace Azure.Deployments.Extensibility.AspNetCore.Behaviors;

/// <summary>
/// Default implementation of <see cref="IResourcePreviewRewriter"/>.
/// </summary>
public class DefaultResourcePreviewRewriter : IResourcePreviewRewriter
{
    /// <inheritdoc/>
    public virtual JsonNode CreateFakeValueForUnevaluatedNode(JsonPointer relativePointer, JsonNode originalValue, ResourcePreviewRewriterContext context)
        => JsonValue.Create("<preview-placeholder>");

    /// <inheritdoc/>
    public virtual JsonNode? MergeValueForUnevaluatedNode(JsonPointer relativePointer, JsonNode originalValue, JsonNode? outgoingValue, ResourcePreviewRewriterContext context)
        => originalValue;

    /// <inheritdoc/>
    public virtual OneOf<ResourcePreview, ErrorResponse> Finalize(ResourcePreview preview) => preview;
}
