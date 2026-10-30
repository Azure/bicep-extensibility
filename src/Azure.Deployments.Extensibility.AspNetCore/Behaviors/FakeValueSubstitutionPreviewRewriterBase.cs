// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
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
public abstract class FakeValueSubstitutionPreviewRewriterBase : IResourcePreviewRewriter
{
    protected const string PropertiesSegment = "properties";
    protected const string ConfigSegment = "config";

    private Dictionary<string, JsonObject>? OriginalCopies { get; set; }
    private ImmutableArray<JsonPointer>? OriginalUnevaluatedPointers { get; set; }
    private bool HandlesRewritingProperties { get; }
    private bool HandlesRewritingConfig { get; }

    protected FakeValueSubstitutionPreviewRewriterBase(bool handlesRewritingProperties, bool handlesRewritingConfig)
    {
        this.HandlesRewritingProperties = handlesRewritingProperties;
        this.HandlesRewritingConfig = handlesRewritingConfig;
    }

    public ResourcePreviewSpecification RewritePreviewRequest(ResourcePreviewSpecification request)
    {
        this.DetermineRewritingScope(request);

        // Before: replace unevaluated ARM template expressions with fake valid values.
        return this.IsRewritingSkipped
            ? request
            : this.ApplyFakeValues(request);
    }

    public OneOf<ResourcePreview, ErrorResponse> RewritePreviewResponse(ResourcePreview response) =>
        // After: restore the original unevaluated property values from the request into the response.
        this.IsRewritingSkipped
            ? response
            : this.Finalize(this.RestoreOriginalValues(response));

    /// <summary>
    /// Produces a fake value for an unevaluated JSON node for an incoming resource preview request.
    /// </summary>
    /// <param name="propertyPointer">A JSON pointer to the property relative to the root of <see cref="ResourcePreviewSpecification"/>.</param>
    /// <param name="originalValue">The original, unevaluated value of the property.</param>
    /// <returns>A fake value to replace the unevaluated property value.</returns>
    /// <remarks>This should be overridden to provide domain-specific acceptable values. For example, creating a value for a string field
    /// with a length requirement or an integer field that must be greater than 0.</remarks>
    protected abstract JsonNode CreateFakeValueForUnevaluatedNode(JsonPointer propertyPointer, JsonNode originalValue);
    
    /// <summary>
    /// Decides the final value to use for a resource preview response JSON property that was received as unevaluated.
    /// </summary>
    /// <param name="propertyPointer">A JSON pointer to the property relative to the root of <see cref="ResourcePreviewSpecification"/>.</param>
    /// <param name="originalValue">The original, unevaluated value of the property.</param>
    /// <param name="outgoingValue">The current outgoing value of the property.</param>
    /// <returns>The final value to use for the property.</returns>
    /// <remarks>Generally this should return the original value unless it is certain that another value should be used.</remarks>
    protected virtual JsonNode? DecideFinalValueForUnevaluatedNode(JsonPointer propertyPointer, JsonNode originalValue, JsonNode? outgoingValue)
        => originalValue;

    /// <summary>
    /// Finalizes the resource preview response.
    /// </summary>
    /// <remarks>Override this to perform complex transformations.</remarks>
    protected virtual OneOf<ResourcePreview, ErrorResponse> Finalize(ResourcePreview preview) => preview;

    /// <summary>
    /// Applies fake values to unevaluated properties of the resource preview specification's properties and config.
    /// </summary>
    private ResourcePreviewSpecification ApplyFakeValues(ResourcePreviewSpecification request)
    {
        if (this.IsRewritingSkipped)
        {
            return request;
        }

        var unevaluatedPointers = request.Metadata.Unevaluated;
        var workingCopies = new Dictionary<string, JsonObject>();
        var operations = new Dictionary<string, List<PatchOperation>>();

        // NOTE: The working copies do not need to be deep-cloned because JsonPatch.Apply() will deep-clone the object.
        if (this.IsRewritingProperties)
        {
            workingCopies.Add(PropertiesSegment, request.Properties);
            operations.Add(PropertiesSegment, []);
        }

        if (this.IsRewritingConfig)
        {
            workingCopies.Add(ConfigSegment, request.Config!);
            operations.Add(ConfigSegment, []);
        }

        // Create fake value replace patch operations
        foreach (var unevaluatedPointer in unevaluatedPointers.Where(p => p.Count > 1))
        {
            var rootObjectPointerSegment = unevaluatedPointer[0];
            var isPropertiesSegment = string.Equals(rootObjectPointerSegment, PropertiesSegment);
            var isConfigSegment = string.Equals(rootObjectPointerSegment, ConfigSegment);

            if (!isPropertiesSegment && !isConfigSegment)
            {
                continue;
            }

            var relativePointer = unevaluatedPointer.GetLocal(1);
            var evalNode = isPropertiesSegment ? request.Properties : request.Config!;

            if (!relativePointer.TryEvaluate(evalNode, out var originalValue) || originalValue is null)
            {
                continue;
            }

            var fakeValue = this.CreateFakeValueForUnevaluatedNode(unevaluatedPointer, originalValue); // Does not need to be deep-cloned, the patch operation will deep-clone it when applied.
            operations[rootObjectPointerSegment].Add(PatchOperation.Replace(relativePointer, fakeValue));
        }

        // Patch
        var patched = false;

        foreach (var (rootObjectPointerSegment, patchOperations) in operations.Where(kvp => kvp.Value.Count > 0))
        {
            var patchResult = new JsonPatch(patchOperations).Apply(workingCopies[rootObjectPointerSegment]);

            if (patchResult is { IsSuccess: true, Result: JsonObject resultObj })
            {
                workingCopies[rootObjectPointerSegment] = resultObj;
                patched = true;
            }
            else
            {
                throw new InvalidOperationException($"Failed to apply patch operations. IsSuccess={patchResult.IsSuccess}; IsNullResult={patchResult.Result is null}; Error={patchResult.Error}");
            }
        }

        return patched
            ? request with
            {
                Config = workingCopies.TryGetValue(ConfigSegment, out var patchedConfig) ? patchedConfig : request.Config,
                Properties = workingCopies.TryGetValue(PropertiesSegment, out var patchedProperties) ? patchedProperties : request.Properties
            }
            : request;
    }

    /// <summary>
    /// Restores original values from the request into the response.
    /// </summary>
    private ResourcePreview RestoreOriginalValues(ResourcePreview preview)
    {
        if (this.IsRewritingSkipped)
        {
            return preview;
        }
        
        ArgumentNullException.ThrowIfNull(this.OriginalUnevaluatedPointers);
        ArgumentNullException.ThrowIfNull(this.OriginalCopies);

        var operations = this.OriginalCopies.ToDictionary(kvp => kvp.Key, _ => new List<PatchOperation>());
        var unevaluatedToRemove = new HashSet<JsonPointer>();

        // Restore original values
        foreach (var unevaluatedPointer in this.OriginalUnevaluatedPointers)
        {
            var rootObjectPointerSegment = unevaluatedPointer[0];
            var isPropertiesSegment = string.Equals(rootObjectPointerSegment, PropertiesSegment);
            var isConfigSegment = string.Equals(rootObjectPointerSegment, ConfigSegment);

            if (!isPropertiesSegment && !isConfigSegment)
            {
                continue;
            }

            var relativePointer = unevaluatedPointer.GetLocal(1);
            var originalEvalNode = this.OriginalCopies[rootObjectPointerSegment];
            var responseEvalNode = isPropertiesSegment ? preview.Properties : preview.Config!;

            if (!relativePointer.TryEvaluate(originalEvalNode, out var originalValue) || originalValue is null)
            {
                // This reflects the continuation of the same condition in ApplyFakeValues.
                continue;
            }

            // If the pointer doesn't evaluate in the response, it means the property was explicitly removed or some higher node was removed,
            // so skip restore processing for this node.
            if (!relativePointer.TryEvaluate(responseEvalNode, out var replacedValue))
            {
                continue;
            }

            // The value in the response should be the fake value, but it's also possible that the property was mutated somewhere between
            // the rewrite and restore (outside the context of this rewriter), so the final value must be decided.
            // Future enhancement: Merge an array in one operation instead of per array item. It would allow customization and handle other
            // complicated array merge scenarios.
            var newValue = this.DecideFinalValueForUnevaluatedNode(unevaluatedPointer, originalValue, replacedValue); // Deep clone is not needed, the patch operation will deep-clone the value.
            operations[rootObjectPointerSegment].Add(PatchOperation.Replace(relativePointer, newValue));

            if (!JsonNode.DeepEquals(originalValue, newValue))
            {
                unevaluatedToRemove.Add(unevaluatedPointer);
            }
        }

        if (operations.All(kvp => kvp.Value.Count is 0))
        {
            return preview;
        }

        // Patch
        var patchedCopies = new Dictionary<string, JsonObject>();

        foreach (var (rootObjectPointerSegment, patchOperations) in operations.Where(kvp => kvp.Value.Count > 0))
        {
            var patchResult = new JsonPatch(patchOperations).Apply(this.OriginalCopies[rootObjectPointerSegment]);

            if (patchResult is { IsSuccess: true, Result: JsonObject resultObj })
            {
                patchedCopies[rootObjectPointerSegment] = resultObj;
            }
            else
            {
                throw new InvalidOperationException($"Failed to apply patch operations. IsSuccess={patchResult.IsSuccess}; IsNullResult={patchResult.Result is null}; Error={patchResult.Error}");
            }
        }

        return patchedCopies.Count is 0
            ? preview
            : preview with
            {
                Config = patchedCopies.TryGetValue(ConfigSegment, out var config) ? config : preview.Config,
                Properties = patchedCopies.TryGetValue(PropertiesSegment, out var properties) ? properties : preview.Properties,
                Metadata = unevaluatedToRemove.Count is 0 || preview.Metadata.Unevaluated is null
                    ? preview.Metadata
                    : preview.Metadata with
                    {
                        Unevaluated = [.. preview.Metadata.Unevaluated.Value.Where(p => !unevaluatedToRemove.Contains(p))]
                    }
            };
    }

    private void DetermineRewritingScope(ResourcePreviewSpecification request)
    {
        this.OriginalCopies = null;
        this.OriginalUnevaluatedPointers = null;
        
        if (request.Metadata.Unevaluated.Length is 0)
        {
            return;
        }

        this.OriginalUnevaluatedPointers = request.Metadata.Unevaluated;

        var isRewritingProperties = this.HandlesRewritingProperties && request.Metadata.Unevaluated.Any(p => p.Count > 1 && string.Equals(p[0], PropertiesSegment, StringComparison.Ordinal));
        var isRewritingConfig = this.HandlesRewritingConfig && request.Config is not null && request.Metadata.Unevaluated.Any(p => p.Count > 1 && string.Equals(p[0], ConfigSegment, StringComparison.Ordinal));

        // NOTE: Deep clones of portions of the original request must be stored to restore the original values because they can be mutated
        // between rewrite and restore.
        if (isRewritingProperties)
        {
            this.OriginalCopies ??= new();
            this.OriginalCopies[PropertiesSegment] = (JsonObject)request.Properties.DeepClone();
        }

        if (isRewritingConfig)
        {
            this.OriginalCopies ??= new();
            this.OriginalCopies[ConfigSegment] = (JsonObject)request.Config!.DeepClone();
        }
    }

    private bool IsRewritingSkipped => this.OriginalCopies?.Count is null or 0;

    private bool IsRewritingProperties => this.OriginalCopies?.ContainsKey(PropertiesSegment) is true;

    private bool IsRewritingConfig => this.OriginalCopies?.ContainsKey(ConfigSegment) is true;
}
