// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Json.Patch;
using Json.Pointer;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Core.V2.Json
{
    /// <summary>
    /// Extension methods for navigating and mutating <see cref="JsonNode"/> trees using JSON Pointers.
    /// </summary>
    public static class JsonNodeExtensions
    {
        /// <summary>
        /// Attempts to retrieve the <see cref="JsonNode"/> at the specified JSON Pointer path.
        /// </summary>
        /// <param name="node">The root node to navigate.</param>
        /// <param name="propertyPath">The JSON Pointer path to the property.</param>
        /// <returns>The <see cref="JsonNode"/> at the path, or <see langword="null"/> if not found.</returns>
        public static JsonNode? TryGetPropertyNode(this JsonNode? node, JsonPointerProxy propertyPath) =>
            TryGetPropertyNode(node, propertyPath.ToJsonPointer());

        /// <inheritdoc cref="TryGetPropertyNode(JsonNode?, JsonPointerProxy)"/>
        public static JsonNode? TryGetPropertyNode(this JsonNode? node, JsonPointer propertyPath)
        {
            ArgumentNullException.ThrowIfNull(node, nameof(node));

            if (propertyPath.TryEvaluate(node, out var propertyNode))
            {
                return propertyNode;
            }

            return default;
        }

        /// <summary>
        /// Retrieves the <see cref="JsonNode"/> at the specified JSON Pointer path.
        /// </summary>
        /// <param name="node">The root node to navigate.</param>
        /// <param name="propertyPath">The JSON Pointer path to the property.</param>
        /// <returns>The <see cref="JsonNode"/> at the path.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the property is not found.</exception>
        public static JsonNode GetPropertyNode(this JsonNode? node, JsonPointerProxy propertyPath) =>
            GetPropertyNode(node, propertyPath.ToJsonPointer());

        /// <inheritdoc cref="GetPropertyNode(JsonNode?, JsonPointerProxy)"/>
        public static JsonNode GetPropertyNode(this JsonNode? node, JsonPointer propertyPath) =>
            TryGetPropertyNode(node, propertyPath) ?? throw new InvalidOperationException($"Property {propertyPath} not found.");

        /// <summary>
        /// Attempts to retrieve a typed value at the specified JSON Pointer path.
        /// </summary>
        /// <typeparam name="T">The expected value type.</typeparam>
        /// <param name="node">The root node to navigate.</param>
        /// <param name="propertyPath">The JSON Pointer path to the property.</param>
        /// <returns>The value at the path, or <see langword="default"/> if not found.</returns>
        public static T? TryGetPropertyValue<T>(this JsonNode? node, JsonPointerProxy propertyPath) =>
            TryGetPropertyValue<T>(node, propertyPath.ToJsonPointer());

        /// <inheritdoc cref="TryGetPropertyValue{T}(JsonNode?, JsonPointerProxy)"/>
        public static T? TryGetPropertyValue<T>(this JsonNode? node, JsonPointer propertyPath) =>
            TryGetPropertyNode(node, propertyPath) is JsonNode propertyNode ? propertyNode.GetValue<T>() : default;

        /// <summary>
        /// Retrieves a typed value at the specified JSON Pointer path.
        /// </summary>
        /// <typeparam name="T">The expected value type.</typeparam>
        /// <param name="node">The root node to navigate.</param>
        /// <param name="propertyPath">The JSON Pointer path to the property.</param>
        /// <returns>The value at the path.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the property is not found.</exception>
        public static T GetPropertyValue<T>(this JsonNode? node, JsonPointerProxy propertyPath) =>
            GetPropertyValue<T>(node, propertyPath.ToJsonPointer());

        /// <inheritdoc cref="GetPropertyValue{T}(JsonNode?, JsonPointerProxy)"/>
        public static T GetPropertyValue<T>(this JsonNode? node, JsonPointer propertyPath) =>
            TryGetPropertyValue<T>(node, propertyPath) ?? throw new InvalidOperationException($"Property {propertyPath} not found.");

        /// <summary>
        /// Returns a new <see cref="JsonNode"/> tree with the property at the specified path set to the given value.
        /// Creates the property if it does not exist. Does not mutate the original node.
        /// </summary>
        /// <param name="node">The root node.</param>
        /// <param name="propertyPath">The JSON Pointer path to the property.</param>
        /// <param name="propertyValue">The value to set.</param>
        /// <returns>A new <see cref="JsonNode"/> with the value applied.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="propertyPath"/> is empty.</exception>
        public static JsonNode WithPropertyValue(this JsonNode? node, JsonPointerProxy propertyPath, JsonNode propertyValue) =>
            WithPropertyValue(node, propertyPath.ToJsonPointer(), propertyValue);

        /// <inheritdoc cref="WithPropertyValue(JsonNode?, JsonPointerProxy, JsonNode)"/>
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

        /// <summary>
        /// Sets the property at the specified JSON Pointer path to the given value in-place,
        /// creating intermediate objects as needed.
        /// </summary>
        /// <param name="node">The root node to mutate.</param>
        /// <param name="propertyPath">The JSON Pointer path to the property.</param>
        /// <param name="propertyValue">The value to set.</param>
        /// <returns>The mutated node.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="propertyPath"/> is empty.</exception>
        public static JsonNode SetPropertyValue(this JsonNode? node, JsonPointerProxy propertyPath, JsonNode propertyValue) =>
            SetPropertyValue(node, propertyPath.ToJsonPointer(), propertyValue);

        /// <inheritdoc cref="SetPropertyValue(JsonNode?, JsonPointerProxy, JsonNode)"/>
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
