// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.AspNetCore.Decorators;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using Json.Patch;
using Json.Pointer;
using System.Collections.Immutable;
using System.Text.Json.Nodes;

namespace MagicEightBallExtension.Decorators;

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
///     <description>Processes metadata to ensure the unevaluated paths from the
///     original request are properly reflected in the response, in addition to
///     any new metadata added by the handler.</description>
///   </item>
/// </list>
/// </summary>
public sealed class PreviewMetadataProcessingDecorator : IResourcePreviewDecorator
{
    private const string PropertiesPrefix = "/properties/";

    public async Task<OneOf<ResourcePreview, ErrorResponse>> HandleAsync(
        ResourcePreviewSpecification request,
        ResourcePreviewHandlerDelegate next,
        CancellationToken cancellationToken)
    {
        var unevaluated = request.Metadata?.Unevaluated;

        // Before: replace unevaluated ARM template expressions with fake valid values.
        if (unevaluated is { Length: > 0 } paths)
        {
            var properties = request.Properties.DeepClone().AsObject();
            properties = ApplyFakeValues(properties, paths);
            request = request with { Properties = properties };
        }

        var response = await next(request);

        // After: ensure the unevaluated paths from the original request
        // are merged into the response metadata.
        if (unevaluated is { Length: > 0 } && response.IsT0)
        {
            var preview = response.AsT0;
            var existingMetadata = preview.Metadata ?? new ResourcePreviewMetadata();

            // Merge the original unevaluated paths into the response metadata.
            var mergedUnevaluated = existingMetadata.Unevaluated is { Length: > 0 } existing
                ? existing.AddRange(unevaluated.Value.Except(existing))
                : unevaluated;

            response = preview with
            {
                Metadata = existingMetadata with { Unevaluated = mergedUnevaluated },
            };
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
}
