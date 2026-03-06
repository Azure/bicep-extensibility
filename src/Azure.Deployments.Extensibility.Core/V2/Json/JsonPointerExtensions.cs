// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Json.Pointer;

namespace Azure.Deployments.Extensibility.Core.V2.Json
{
    public static class JsonPointerExtensions
    {
        public static string? LastSegment(this JsonPointer pointer) => pointer.Count > 0 ? pointer[^1] : null;
        
        public static bool IsDescendantOf(this JsonPointer pointer, JsonPointer parent) =>
            parent.Count < pointer.Count && pointer.GetAncestor(pointer.Count - parent.Count) == parent;

        public static bool IsDescendantOf(this JsonPointer pointer, JsonPointer parent, [NotNullWhen(true)] out JsonPointer? relativePointer)
        {
            relativePointer = null;

            if (!pointer.IsDescendantOf(parent))
            {
                return false;
            }

            relativePointer = pointer.GetSubPointer(new Range(parent.Count, pointer.Count));

            return true;
        }

        public static IEnumerable<JsonPointer> CollectAllArrayPaths(this JsonPointer pointer, JsonNode root)
        {
            // Start at the root node at work 
            for (var depth = 1; depth <= pointer.Count; depth++)
            {
                var currentPointer = pointer.GetSubPointer(..depth);

                if (currentPointer.TryEvaluate(root, out var intermediateNode))
                {
                    if (intermediateNode is JsonArray)
                    {
                        yield return currentPointer;
                    }
                }
                else
                {
                    break;
                }
            }
        }

        public static bool TryRemove(this JsonPointer pointer, JsonNode node, [NotNullWhen(true)] out JsonNode? removedNode)
        {
            removedNode = null;

            if (!pointer.TryEvaluate(node, out var nodeToRemove) || nodeToRemove is null)
            {
                return false;
            }

            switch (nodeToRemove.Parent)
            {
                case JsonObject parentObj:
                    removedNode = nodeToRemove;

                    return parentObj.Remove(pointer.LastSegment()!);
                case JsonArray parentArr:
                    removedNode = nodeToRemove;

                    return parentArr.Remove(nodeToRemove);
                default:
                    return false;
            }
        }
    }
}
