// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.AspNetCore.Behaviors;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using Json.Patch;
using Json.Pointer;
using System.Collections.Immutable;
using System.Text.Json.Nodes;

namespace MagicEightBallExtension.Behaviors;

/// <summary>
/// A per-handler decorator for preview operations.
/// <list type="bullet">
///   <item>
///     <term>Before the inner handler:</term>
///     <description>Replaces unevaluated property values (ARM template expressions)
///     with fake valid values so downstream validation logic can pass.</description>
///   </item>
///   <item>
///     <term>After the inner handler:</term>
///     <description>Restores the original unevaluated property values from the request
///     into the response properties, so ARM template expressions are echoed back as-is.
///     The handler is responsible for including the unevaluated paths in the response metadata.</description>
///   </item>
/// </list>
/// </summary>
public sealed class PreviewMetadataProcessingBehavior : IResourcePreviewBehavior
{
    private const string PropertiesPrefix = "/properties/";

    public async Task<OneOf<ResourcePreview, ErrorResponse>> HandleAsync(
        ResourcePreviewSpecification request,
        ResourcePreviewHandlerDelegate next,
        CancellationToken cancellationToken)
    {
        var unevaluated = request.Metadata?.Unevaluated;
        var originalProperties = request.Properties;

        // Before: replace unevaluated ARM template expressions with fake valid values.
        if (unevaluated is { Length: > 0 } paths)
        {
            var properties = request.Properties.DeepClone().AsObject();
            properties = ApplyFakeValues(properties, paths);
            request = request with { Properties = properties };
        }

        var response = await next(request);

        // After: restore the original unevaluated property values from the request into the response.
        if (unevaluated is { Length: > 0 } && response.IsT0)
        {
            var preview = response.AsT0;
            var restoredProperties = RestoreOriginalValues(preview.Properties.DeepClone().AsObject(), originalProperties, unevaluated.Value);
            response = preview with { Properties = restoredProperties };
        }

        return response;
    }

    /// <summary>
    /// Builds a <see cref="JsonPatch"/> that replaces every reachable unevaluated
    /// path under <c>/properties/</c> with a valid placeholder value, then applies it.
    /// </summary>
    private static JsonObject ApplyFakeValues(JsonObject properties, ImmutableArray<JsonPointer> pointers)
    {
        var operations = new List<PatchOperation>();

        foreach (var pointer in pointers)
        {
            var path = pointer.ToString();

            if (!path.StartsWith(PropertiesPrefix, StringComparison.Ordinal))
            {
                continue;
            }

            var relativePointer = JsonPointer.Parse("/" + path[PropertiesPrefix.Length..]);

            if (relativePointer.TryEvaluate(properties, out _))
            {
                // In real world scenarios, you might want to generate different dummy values based on the expected type of the property.
                operations.Add(PatchOperation.Replace(relativePointer, JsonValue.Create("<preview-placeholder>")));
            }
        }

        if (operations.Count == 0)
        {
            return properties;
        }

        var patch = new JsonPatch([.. operations]);
        var result = patch.Apply(properties);

        return result.Result?.AsObject() ?? properties;
    }

    /// <summary>
    /// Builds a <see cref="JsonPatch"/> that restores every reachable unevaluated
    /// path under <c>/properties/</c> to its original value from the request, then applies it.
    /// </summary>
    private static JsonObject RestoreOriginalValues(JsonObject responseProperties, JsonNode originalProperties, ImmutableArray<JsonPointer> pointers)
    {
        var operations = new List<PatchOperation>();

        foreach (var pointer in pointers)
        {
            var path = pointer.ToString();

            if (!path.StartsWith(PropertiesPrefix, StringComparison.Ordinal))
            {
                continue;
            }

            var relativePointer = JsonPointer.Parse("/" + path[PropertiesPrefix.Length..]);

            if (relativePointer.TryEvaluate(originalProperties, out var originalValue) &&
                relativePointer.TryEvaluate(responseProperties, out _))
            {
                operations.Add(PatchOperation.Replace(relativePointer, originalValue!.DeepClone()));
            }
        }

        if (operations.Count == 0)
        {
            return responseProperties;
        }

        var patch = new JsonPatch([.. operations]);
        var result = patch.Apply(responseProperties);

        return result.Result?.AsObject() ?? responseProperties;
    }
}
