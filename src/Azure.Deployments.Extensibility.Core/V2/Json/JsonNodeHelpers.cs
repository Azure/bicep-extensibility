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
            var allArrayPaths = new HashSet<JsonPointer>();

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

                // If it's not valid, do nothing
                if (!pathToRemove.TryEvaluate(@object, out var nodeToRemove))
                {
                    continue;
                }

                // Collect all the array paths. These paths will be stored with the filtered JSON object for reconstruction purposes.
                foreach (var arrPath in pathToRemove.CollectAllArrayPaths(@object))
                {
                    allArrayPaths.Add(arrPath);
                }

                // Remove the node.
                var nodeWasRemoved = false;

                if (nodeToRemove is not null)
                {
                    nodeToRemove.RemoveFromParent();
                    nodeWasRemoved = true;
                }
                else if (pathToRemove.Count > 1 && pathToRemove.GetAncestor(1).TryEvaluate(@object, out var parentNodeOfRemoval))
                {
                    // removing a `null` object property or array element.
                    switch (parentNodeOfRemoval)
                    {
                        case JsonObject parentObj:
                            parentObj.Remove(pathToRemove[^1]);
                            nodeWasRemoved = true;

                            break;
                        case JsonArray parentArr:
                            parentArr.RemoveAt(Int32.Parse(pathToRemove[^1]));
                            nodeWasRemoved = true;

                            break;
                    }
                }

                // Reconstruct the node in the "RemovedObject". The object's schema mirrors that of the input object except arrays are
                // stored as dictionaries of indices to values. In combination with the cached array paths, the "RemovedObject" can be
                // merged back into the filtered object at a later time.
                if (nodeWasRemoved)
                {
                    removed.SetPropertyValue(pathToRemove, nodeToRemove);
                    mutated = true;
                }
            }

            return new FilteredJsonObject(@object, removed.Count > 0 ? removed : null, allArrayPaths);
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
    }
}
