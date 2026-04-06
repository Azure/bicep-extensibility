// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using Json.Patch;
using Json.Pointer;

namespace Azure.Deployments.Extensibility.AspNetCore.Behaviors;

/// <summary>
/// A preview behavior for handling resource preview operations where unevaluated Azure Resource Manager (ARM) template expressions
/// are replaced with fake valid values before progressing in the request pipeline. After a response is received in the pipeline, fake values
/// are replaced with original unevaluated values.
/// </summary>
/// <remarks>
/// This behavior integrates with the resource preview pipeline and ensures that any unevaluated
/// dynamic expressions in the request are substituted with fake placeholder values prior to handling the preview request.
/// After the processing is complete, it restores the original unevaluated values in the response to maintain consistency.
/// </remarks>
/// <example>
/// Configure and use this behavior as part of a resource preview request pipeline, passing an optional custom
/// implementation of <see cref="IResourcePreviewRewriter"/> to override default rewriter behavior.
/// </example>
public class PreviewUnevaluatedProcessingBehavior : IResourcePreviewBehavior
{
    private const string PropertiesSegment = "properties";
    private const string PropertiesPrefix = $"/{PropertiesSegment}/";

    private IResourcePreviewRewriter PreviewRewriter { get; }

    public PreviewUnevaluatedProcessingBehavior(IResourcePreviewRewriter? previewRewriter = null)
    {
        this.PreviewRewriter = previewRewriter ?? new DefaultResourcePreviewRewriter();
    }

    public async Task<OneOf<ResourcePreview, ErrorResponse>> HandleAsync(
        ResourcePreviewSpecification request,
        ResourcePreviewHandlerDelegate next,
        CancellationToken cancellationToken)
    {
        // Before: replace unevaluated ARM template expressions with fake valid values.
        var originalProperties = request.Properties.DeepClone().AsObject();

        request = this.ApplyFakeValues(request);

        var response = await next(request);

        // After: restore the original unevaluated property values from the request into the response.
        if (response.IsT0)
        {
            var outgoingPreview = this.RestoreOriginalValues(response.AsT0, request, originalProperties);
            response = this.PreviewRewriter.Finalize(outgoingPreview);
        }

        return response;
    }

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
        var context = new ResourcePreviewRewriterContext
        {
            ResourceType = request.Type,
            ApiVersion = request.ApiVersion,
            RootPointer = JsonPointer.Parse("/" + PropertiesSegment),
            RootObject = properties
        };

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
                var fakeValue = this.PreviewRewriter.CreateFakeValueForUnevaluatedNode(relativePointer, originalValue, context)?.DeepClone();
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
    private ResourcePreview RestoreOriginalValues(ResourcePreview preview, ResourcePreviewSpecification request, JsonObject originalProperties)
    {
        if (request.Metadata?.Unevaluated is not { Length: > 0 } unevaluated)
        {
            return preview;
        }

        var responseProperties = preview.Properties.DeepClone().AsObject();
        var context = new ResourcePreviewRewriterContext
        {
            ResourceType = request.Type,
            ApiVersion = request.ApiVersion,
            RootPointer = JsonPointer.Parse("/" + PropertiesSegment),
            RootObject = responseProperties
        };

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

            if (!relativePointer.TryEvaluate(originalProperties, out var originalValue) || originalValue is null || !relativePointer.TryEvaluate(responseProperties, out var replacedValue))
            {
                continue; // The original property value should be a language expression; the replaced value should be a fake value applied earlier.
            }

            // Future enhancement: Merge an array in one operation instead of per array item. It would allow customization and handle other
            // complicated array merge scenarios.
            var newValue = this.PreviewRewriter.MergeValueForUnevaluatedNode(relativePointer, originalValue, replacedValue, context)?.DeepClone();
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
                    ? [..request.Metadata.Unevaluated.Where(p => !unevaluatedToRemove.Contains(p))]
                    : request.Metadata.Unevaluated
            }
        };
    }
}
