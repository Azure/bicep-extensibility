// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Azure.Deployments.Extensibility.AspNetCore.Behaviors;
using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using Json.Patch;
using Json.Pointer;

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
    /// <summary>
    /// A delegate that provides fake values for unevaluated property values in a resource preview context.
    /// </summary>
    /// <param name="jsonPointer">A JSON pointer indicating the location of the property in the resource properties tree.</param>
    /// <param name="originalValue">The original, unevaluated value of the property.</param>
    /// <param name="resourceType">The type of the resource being processed.</param>
    /// <param name="apiVersion">The API version of the resource.</param>
    /// <returns>A fake value to replace the unevaluated property.</returns>
    public delegate JsonNode? FakeValueProviderDelegate(JsonPointer jsonPointer, JsonNode? originalValue, string resourceType, string? apiVersion);

    /// <summary>
    /// A delegate that determines the final value for a resource preview property before the preview is returned.
    /// </summary>
    /// <param name="jsonPointer">A JSON pointer indicating the location of the property in the resource properties tree.</param>
    /// <param name="originalValue">The original, unevaluated value of the property.</param>
    /// <param name="newValue">The current outgoing value of the property.</param>
    /// <param name="resourceType">The type of the resource being processed.</param>
    /// <param name="apiVersion">The API version of the resource.</param>
    /// <returns>The final value to use for the property.</returns>
    public delegate JsonNode? MergeValueDelegate(JsonPointer jsonPointer, JsonNode? originalValue, JsonNode? newValue, string resourceType, string? apiVersion);

    public static FakeValueProviderDelegate DefaultFakeValueProvider { get; } = (_, _, _, _) => JsonValue.Create("<preview-placeholder>");
    public static MergeValueDelegate DefaultMergeValueProvider { get; } = (_, originalValue, _, _, _) => originalValue;

    private FakeValueProviderDelegate FakeValueProvider { get; }

    private MergeValueDelegate MergeValueProvider { get; }
                                                
    private const string PropertiesPrefix = "/properties/";

    public PreviewMetadataProcessingBehavior(FakeValueProviderDelegate? fakeValueProvider = null, MergeValueDelegate? mergeValueProvider = null)
    {
        this.FakeValueProvider = fakeValueProvider ?? DefaultFakeValueProvider;
        this.MergeValueProvider = mergeValueProvider ?? DefaultMergeValueProvider;
    }

    public async Task<OneOf<ResourcePreview, ErrorResponse>> HandleAsync(
        ResourcePreviewSpecification request,
        ResourcePreviewHandlerDelegate next,
        CancellationToken cancellationToken)
    {
        var unevaluated = request.Metadata?.Unevaluated;

        // Before: replace unevaluated ARM template expressions with fake valid values.
        if (unevaluated is { Length: > 0 } paths)
        {
            var properties = this.ApplyFakeValues(request, paths);
            request = request with { Properties = properties };
        }

        var response = await next(request);

        // After: restore the original unevaluated property values from the request into the response.
        if (unevaluated is { Length: > 0 } && response.IsT0)
        {
            var preview = response.AsT0;
            var restoredProperties = this.RestoreOriginalValues(preview.Properties.DeepClone().AsObject(), request, unevaluated.Value);
            response = preview with { Properties = restoredProperties };
        }

        return response;
    }

    /// <summary>
    /// Builds a <see cref="JsonPatch"/> that replaces every reachable unevaluated
    /// path under <c>/properties/</c> with a valid placeholder value, then applies it.
    /// </summary>
    private JsonObject ApplyFakeValues(ResourcePreviewSpecification request, ImmutableArray<JsonPointer> pointers)
    {
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

            if (relativePointer.TryEvaluate(properties, out var originalValue))
            {
                var newValue = this.FakeValueProvider.Invoke(relativePointer, originalValue, request.Type, request.ApiVersion)?.DeepClone();
                operations.Add(PatchOperation.Replace(relativePointer, newValue));
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
    private JsonObject RestoreOriginalValues(JsonObject responseProperties, ResourcePreviewSpecification request, ImmutableArray<JsonPointer> pointers)
    {
        var originalProperties = request.Properties.DeepClone().AsObject();
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
                relativePointer.TryEvaluate(responseProperties, out var replacedValue))
            {
                var newValue = this.MergeValueProvider.Invoke(relativePointer, originalValue, replacedValue, request.Type, request.ApiVersion)?.DeepClone();
                operations.Add(PatchOperation.Replace(relativePointer, newValue));
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
