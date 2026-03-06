// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Json;
using Azure.Deployments.Extensibility.Core.V2.Models;
using Json.Pointer;

namespace Azure.Deployments.Extensibility.Core.V2.Helpers
{
    public static class PreviewResourceHelpers
    {
        /// <summary>
        /// Removes unevaluated paths from the provided resource specification if specified in its metadata.
        /// </summary>
        /// <param name="spec">The resource specification that contains properties, configuration, and metadata, where unevaluated paths
        /// are identified and removed during processing.</param>
        /// <returns>A <see cref="FilteredPreviewResourceSpecification"/> that can be later remerged.</returns>
        /// <remarks>This is intended to be used on the incoming resource spec from the Deployments engine to assist with filtering out
        /// language expression nodes from the spec JSON.</remarks>
        public static FilteredPreviewResourceSpecification FilterUnevaluatedPaths(ResourceSpecification spec)
        {
            if (spec.Metadata?.Unevaluated?.Any() is not true)
            {
                return new FilteredPreviewResourceSpecification(spec);
            }

            var changed = false;

            // Process config.
            FilteredJsonObject? configResult = null;

            if (spec.Config is not null && FindParentPointerInsensitively(spec.Metadata.Unevaluated, JsonPointer.Parse($"#/{nameof(ResourceSpecification.Config)}")) is { } configPointer)
            {
                configResult = JsonNodeHelpers.RemovePathsNullable(spec.Config, spec.Metadata.Unevaluated, out var configMutated, configPointer);
                changed |= configMutated;
            }

            // Process properties.
            FilteredJsonObject? propsResult = null;

            if (FindParentPointerInsensitively(spec.Metadata.Unevaluated, JsonPointer.Parse($"#/{nameof(ResourceSpecification.Properties)}")) is { } propsPointer)
            {
                propsResult = JsonNodeHelpers.RemovePaths(spec.Properties, spec.Metadata.Unevaluated, out var propsMutated, propsPointer);
                changed |= propsMutated;
            }

            return new FilteredPreviewResourceSpecification(
                filteredSpec: !changed
                    ? spec
                    : spec with
                    {
                        Config = configResult?.FilteredObject ?? spec.Config,
                        Properties = propsResult?.FilteredObject ?? spec.Properties
                    },
                filteredConfig: configResult,
                filteredProperties: propsResult);
        }

        private static JsonPointer? FindParentPointerInsensitively(IEnumerable<JsonPointer>? pointers, JsonPointer find) =>
            pointers?.FirstOrDefault(p => p.Count > find.Count
                && Enumerable.Range(0, find.Count).All(i => string.Equals(p[i], find[i], StringComparison.OrdinalIgnoreCase)));
    }
}
