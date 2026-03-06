// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using Azure.Deployments.Extensibility.Core.V2.Models;
using Json.Pointer;

namespace Azure.Deployments.Extensibility.Core.V2.Json
{
    public static class JsonNodeHelpers
    {
        /// <summary>
        /// Removes JSON nodes from a JSON object using the provided JSON pointers, returning the modified object and a separate
        /// reconstructed object that reflects the schema of the input object but contains only the nodes removed.
        /// </summary>
        /// <param name="object">The JSON object from which unevaluated paths will be removed.</param>
        /// <param name="pathsToRemove">A collection of JSON pointers representing the paths to be removed.</param>
        /// <param name="mutated">The out value is set to true if the object was mutated, false otherwise.</param>
        /// <param name="parentPath">An optional JSON pointer indicating the parent path scope if the provided paths to remove are relative to another node.</param>
        /// <returns>A tuple containing the modified object and a separate reconstructed object that reflects the schema of the input
        /// object but contains only the nodes removed.</returns>
        public static FilteredJsonObject RemovePaths(
            JsonObject @object,
            IEnumerable<JsonPointer>? pathsToRemove,
            out bool mutated,
            JsonPointer? parentPath = null)
        {
            mutated = false;

            // ReSharper disable once PossibleMultipleEnumeration
            if (pathsToRemove?.Any() is not true)
            {
                return new FilteredJsonObject(@object);
            }

            var removed = new JsonObject();
            var arrayPaths = new HashSet<JsonPointer>();

            // ReSharper disable once PossibleMultipleEnumeration
            foreach (var candidatePath in pathsToRemove)
            {
                var pathToRemove = candidatePath;

                // Get a relative pointer if a parent path is provided
                if (parentPath is not null)
                {
                    if (pathToRemove.IsDescendantOf(parentPath, out var relativePointer))
                    {
                        pathToRemove = relativePointer;
                    }
                    else
                    {
                        continue; // not relevant for this operation.
                    }
                }

                // Find the node and remove it.
                if (pathToRemove.TryRemove(@object, out var removedNode))
                {
                    mutated = true;
                    // TODO(kylealbert): handle arrays
                    removed.SetPropertyValue(pathToRemove, removedNode);
                }
            }

            return new FilteredJsonObject(@object, removed.Count > 0 ? removed : null, arrayPaths);
        }

        /// <inheritdoc cref="RemovePaths" />
        public static FilteredJsonObject? RemovePathsNullable(
            JsonObject? @object,
            IEnumerable<JsonPointer>? pathsToRemove,
            out bool mutated,
            JsonPointer? parentPath = null)
        {
            mutated = false;

            return @object is not null ? RemovePaths(@object, pathsToRemove, out mutated, parentPath) : null;
        }

        public static JsonObject Merge(JsonObject @base, JsonObject toMerge)
        {
            // TODO(kylealbert): implement
            return @base;
        }
    }
}
