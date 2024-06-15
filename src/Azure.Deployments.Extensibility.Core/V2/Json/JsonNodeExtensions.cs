// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Json.Patch;
using Json.Pointer;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Core.V2.Json
{
    public static class JsonNodeExtensions
    {
        public static JsonNode? TryGetPropertyNode(this JsonNode? node, JsonPointerProxy propertyPath) =>
            TryGetPropertyNode(node, propertyPath.ToJsonPointer());

        public static JsonNode? TryGetPropertyNode(this JsonNode? node, JsonPointer propertyPath)
        {
            ArgumentNullException.ThrowIfNull(node, nameof(node));

            if (propertyPath.TryEvaluate(node, out var propertyNode))
            {
                return propertyNode;
            }

            return default;
        }

        public static JsonNode GetPropertyNode(this JsonNode? node, JsonPointerProxy propertyPath) =>
            GetPropertyNode(node, propertyPath.ToJsonPointer());

        public static JsonNode GetPropertyNode(this JsonNode? node, JsonPointer propertyPath) =>
            TryGetPropertyNode(node, propertyPath) ?? throw new InvalidOperationException($"Property {propertyPath} not found.");

        public static T? TryGetPropertyValue<T>(this JsonNode? node, JsonPointerProxy propertyPath) =>
            TryGetPropertyValue<T>(node, propertyPath.ToJsonPointer());

        public static T? TryGetPropertyValue<T>(this JsonNode? node, JsonPointer propertyPath) =>
            TryGetPropertyNode(node, propertyPath) is JsonNode propertyNode ? propertyNode.GetValue<T>() : default;

        public static T GetPropertyValue<T>(this JsonNode? node, JsonPointerProxy propertyPath) =>
            GetPropertyValue<T>(node, propertyPath.ToJsonPointer());

        public static T GetPropertyValue<T>(this JsonNode? node, JsonPointer propertyPath) =>
            TryGetPropertyValue<T>(node, propertyPath) ?? throw new InvalidOperationException($"Property {propertyPath} not found.");

        public static JsonNode WithPropertyValue(this JsonNode? node, JsonPointerProxy propertyPath, JsonNode propertyValue) =>
            WithPropertyValue(node, propertyPath.ToJsonPointer(), propertyValue);

        public static JsonNode WithPropertyValue(this JsonNode? node, JsonPointer propertyPath, JsonNode propertyValue)
        {
            ArgumentNullException.ThrowIfNull(node, nameof(node));

            if (propertyPath.Count == 0)
            {
                throw new ArgumentException("Argument cannot be empty.", nameof(propertyPath));
            }

            var patch = new JsonPatch(PatchOperation.Add(propertyPath, propertyValue));
            var patchResult = patch.Apply(node);

            return patchResult.Result ?? throw new InvalidOperationException(patchResult.Error);
        }

        public static JsonNode SetPropertyValue(this JsonNode? node, JsonPointerProxy propertyPath, JsonNode propertyValue) =>
            SetPropertyValue(node, propertyPath.ToJsonPointer(), propertyValue);

        public static JsonNode SetPropertyValue(this JsonNode? node, JsonPointer propertyPath, JsonNode propertyValue)
        {
            ArgumentNullException.ThrowIfNull(node, nameof(node));

            if (propertyPath.Count == 0)
            {
                throw new ArgumentException("Argument cannot be empty.", nameof(propertyPath));
            }

            for (int i = 0; i < propertyPath.Count; i++)
            {
                var segment = propertyPath[i];

                if (i != propertyPath.Count - 1)
                {
                    if (node is not JsonObject jsonObject)
                    {
                        throw new InvalidOperationException($"Property {segment} not found.");
                    }

                    if (node[segment] is not { } propertyNode)
                    {
                        propertyNode = new JsonObject();
                        jsonObject[segment] = propertyNode;
                    }

                    node = propertyNode.AsObject();
                }
                else
                {
                    node[segment] = propertyValue;
                }
            }

            return node;
        }
    }
}
