// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Text.Json.Nodes;
using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using Json.Patch;
using Json.Pointer;

namespace Azure.Deployments.Extensibility.AspNetCore.Behaviors;

/// <summary>
/// Rewrites resource preview requests and responses by substituting unevaluated template expressions
/// with fake placeholder values and restoring the original values during the response rewrite.
/// </summary>
public class FakeValueSubstitutionPreviewRewriter : IResourcePreviewRewriter
{
    protected const string PropertiesSegment = "properties";
    protected const string PropertiesPrefix = $"/{PropertiesSegment}/";

    private ConcurrentDictionary<ResourcePreviewSpecification, Context> RequestContext { get; } = new(ReferenceEqualityComparer.Instance);

    public ResourcePreviewSpecification RewritePreviewRequest(ResourcePreviewSpecification request)
    {
        // Before: replace unevaluated ARM template expressions with fake valid values.
        var originalProperties = request.Properties.DeepClone().AsObject();

        var newRequest = this.ApplyFakeValues(request);

        if (!ReferenceEquals(request, newRequest))
        {
            if (!this.RequestContext.TryAdd(request, new Context(request, originalProperties)))
            {
                throw new InvalidOperationException("Context already exists for request.");
            }
        }

        return newRequest;
    }

    public OneOf<ResourcePreview, ErrorResponse> RewritePreviewResponse(ResourcePreview response, ResourcePreviewSpecification originalRequest)
    {
        if (!this.RequestContext.TryRemove(originalRequest, out var context))
        {
            throw new InvalidOperationException("Context not found for request.");
        }
        
        // After: restore the original unevaluated property values from the request into the response.
        var outgoingPreview = this.RestoreOriginalValues(response, context);

        return this.Finalize(outgoingPreview, context);
    }

    /// <summary>
    /// Produces a fake value for an unevaluated JSON node for an incoming resource preview request.
    /// </summary>
    /// <param name="relativePointer">A JSON pointer indicating the location of the property in the current rewrite context.</param>
    /// <param name="originalValue">The original, unevaluated value of the property.</param>
    /// <param name="originalRequest">The original request.</param>
    /// <returns>A fake value to replace the unevaluated property value.</returns>
    /// <remarks>This should be overridden to provide domain-specific acceptable values. For example, creating a value for a string field
    /// with a length requirement or an integer field that must be greater than 0.</remarks>
    protected virtual JsonNode CreateFakeValueForUnevaluatedNode(JsonPointer relativePointer, JsonNode originalValue, ResourcePreviewSpecification originalRequest)
        => JsonValue.Create("<preview-placeholder>");

    /// <summary>
    /// Decides the final value to use for a resource preview response JSON property that was received as unevaluated.
    /// </summary>
    /// <param name="relativePointer">A JSON pointer indicating the location of the property in the resource properties tree.</param>
    /// <param name="originalValue">The original, unevaluated value of the property.</param>
    /// <param name="outgoingValue">The current outgoing value of the property.</param>
    /// <param name="context">The context of the resource preview request.</param>
    /// <returns>The final value to use for the property.</returns>
    /// <remarks>Generally this should return the original value unless it is certain that another value should be used.</remarks> 
    protected virtual JsonNode? MergeValueForUnevaluatedNode(JsonPointer relativePointer, JsonNode originalValue, JsonNode? outgoingValue, Context context)
        => originalValue;

    /// <summary>
    /// Finalizes the resource preview response.
    /// </summary>
    /// <remarks>Override this to perform complex transformations.</remarks>
    protected virtual OneOf<ResourcePreview, ErrorResponse> Finalize(ResourcePreview preview, Context context) => preview;

    /// <summary>
    /// Builds a <see cref="JsonPatch"/> that replaces every reachable unevaluated
    /// path under <c>/properties/</c> with a valid placeholder value, then applies it.
    /// </summary>
    private ResourcePreviewSpecification ApplyFakeValues(ResourcePreviewSpecification request)
    {
        var pointers = request.Metadata?.Unevaluated;

        if (pointers is not { Length: > 0 })
        {
            return request;
        }

        var properties = request.Properties.DeepClone().AsObject();
        var operations = new List<PatchOperation>();

        foreach (var pointer in pointers)
        {
            var path = pointer.ToString();

            if (!path.StartsWith(PropertiesPrefix, StringComparison.Ordinal))
            {
                continue;
            }

            var relativePointer = JsonPointer.Parse("/" + path[PropertiesPrefix.Length..]);

            if (relativePointer.TryEvaluate(properties, out var originalValue) && originalValue is not null)
            {
                var fakeValue = this.CreateFakeValueForUnevaluatedNode(relativePointer, originalValue, request)?.DeepClone();
                operations.Add(PatchOperation.Replace(relativePointer, fakeValue));
            }
        }

        if (operations.Count == 0)
        {
            return request;
        }

        var patch = new JsonPatch([.. operations]);
        var result = patch.Apply(properties);

        return request with
        {
            Properties = result.Result?.AsObject() ?? properties
        };
    }

    /// <summary>
    /// Builds a <see cref="JsonPatch"/> that restores every reachable unevaluated
    /// path under <c>/properties/</c> to its original value from the request, then applies it.
    /// </summary>
    private ResourcePreview RestoreOriginalValues(ResourcePreview preview, Context context)
    {
        if (context.OriginalRequest.Metadata?.Unevaluated is not { Length: > 0 } unevaluated)
        {
            return preview;
        }
        
        var responseProperties = preview.Properties.DeepClone().AsObject();
        var operations = new List<PatchOperation>();
        var unevaluatedToRemove = new HashSet<JsonPointer>();

        foreach (var pointer in unevaluated)
        {
            var path = pointer.ToString();

            if (!path.StartsWith(PropertiesPrefix, StringComparison.Ordinal))
            {
                continue;
            }

            var relativePointer = JsonPointer.Parse("/" + path[PropertiesPrefix.Length..]);

            if (!relativePointer.TryEvaluate(context.OriginalProperties, out var originalValue) || originalValue is null || !relativePointer.TryEvaluate(responseProperties, out var replacedValue))
            {
                continue; // The original property value should be a language expression; the replaced value should be a fake value applied earlier.
            }

            // Future enhancement: Merge an array in one operation instead of per array item. It would allow customization and handle other
            // complicated array merge scenarios.
            var newValue = this.MergeValueForUnevaluatedNode(relativePointer, originalValue, replacedValue, context)?.DeepClone();
            operations.Add(PatchOperation.Replace(relativePointer, newValue));

            if (!JsonNode.DeepEquals(originalValue, newValue))
            {
                unevaluatedToRemove.Add(pointer);
            }
        }

        if (operations.Count == 0)
        {
            return preview;
        }

        var patch = new JsonPatch([.. operations]);
        var result = patch.Apply(responseProperties);

        return preview with
        {
            Properties = result.Result?.AsObject() ?? responseProperties,
            Metadata = (preview.Metadata ?? new()) with
            {
                Unevaluated = unevaluatedToRemove.Count > 0
                    ? [..context.OriginalRequest.Metadata.Unevaluated.Where(p => !unevaluatedToRemove.Contains(p))]
                    : context.OriginalRequest.Metadata.Unevaluated
            }
        };
    }

    protected class Context
    {
        public ResourcePreviewSpecification OriginalRequest { get; init; }

        public JsonObject OriginalProperties { get; init; }

        public Context(ResourcePreviewSpecification originalRequest, JsonObject originalProperties)
        {
            this.OriginalRequest = originalRequest;
            this.OriginalProperties = originalProperties;
        }
    }
}
