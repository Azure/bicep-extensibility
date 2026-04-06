// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using Json.Pointer;

namespace Azure.Deployments.Extensibility.AspNetCore.Behaviors;

public readonly record struct ResourcePreviewRewriterContext
{
    /// <summary>The resource type of the resource being previewed.</summary>
    public string ResourceType { get; init; }

    /// <summary>The API version of the resource being previewed.</summary>
    public string? ApiVersion { get; init; }

    /// <summary>The JSON pointer of the top-most node being modified in the resource preview request payload. Any relative pointers are
    /// relative to this pointer.</summary>
    /// <example>/properties</example>
    public JsonPointer RootPointer { get; init; }

    /// <summary>The top-most JSON object of the rewrite operation.</summary>
    /// <remarks>Do not modify this object.</remarks>
    public JsonObject RootObject { get; init; }
}

/// <summary>
/// An interface for rewriting resource preview requests and responses.
/// </summary>
/// <remarks>
/// This interface provides mechanisms to generate or modify property values during resource preview
/// requests. Implementations can produce synthetic values for unevaluated JSON nodes as well as
/// merge or override existing values in a domain-specific manner.
/// </remarks>
public interface IResourcePreviewRewriter
{
    /// <summary>
    /// Produces a fake value for an unevaluated JSON node for an incoming resource preview request.
    /// </summary>
    /// <param name="relativePointer">A JSON pointer indicating the location of the property in the current rewrite context.</param>
    /// <param name="originalValue">The original, unevaluated value of the property.</param>
    /// <param name="context">The context of the resource preview request.</param>
    /// <returns>A fake value to replace the unevaluated property value.</returns>
    /// <remarks>This should be overridden to provide domain-specific acceptable values. For example, creating a value for a string field
    /// with a length requirement or an integer field that must be greater than 0.</remarks>
    JsonNode CreateFakeValueForUnevaluatedNode(JsonPointer relativePointer, JsonNode originalValue, ResourcePreviewRewriterContext context);

    /// <summary>
    /// Decides the final value to use for a resource preview response JSON property that was received as unevaluated.
    /// </summary>
    /// <param name="relativePointer">A JSON pointer indicating the location of the property in the resource properties tree.</param>
    /// <param name="originalValue">The original, unevaluated value of the property.</param>
    /// <param name="outgoingValue">The current outgoing value of the property.</param>
    /// <param name="context">The context of the resource preview request.</param>
    /// <returns>The final value to use for the property.</returns>
    /// <remarks>Generally this should return the original value unless it is certain that another value should be used.</remarks> 
    JsonNode? MergeValueForUnevaluatedNode(JsonPointer relativePointer, JsonNode originalValue, JsonNode? outgoingValue, ResourcePreviewRewriterContext context);

    /// <summary>Finalizes the resource preview response. This is the very last step before the response continues through the response pipeline.</summary>
    /// <remarks>This can be overriden to perform more complex transformations.</remarks>
    OneOf<ResourcePreview, ErrorResponse> Finalize(ResourcePreview preview);
}
