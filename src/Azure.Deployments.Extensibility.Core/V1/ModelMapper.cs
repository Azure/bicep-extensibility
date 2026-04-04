// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.Json;
using System.Text.Json;

namespace Azure.Deployments.Extensibility.Core
{
    /// <summary>
    /// Provides helper methods for converting between generic (<see cref="JsonElement"/>-based) and
    /// strongly-typed extensibility models.
    /// </summary>
    public static class ModelMapper
    {
        /// <summary>Deserialize an import from <see cref="JsonElement"/> properties to a concrete type.</summary>
        public static ExtensibleImport<T> MapToConcrete<T>(ExtensibleImport<JsonElement> import) =>
            new(import.Provider, import.Version, Deserialize<T>(import.Config));

        /// <summary>Serialize an import from a concrete type to <see cref="JsonElement"/> properties.</summary>
        public static ExtensibleImport<JsonElement> MapToGeneric<T>(ExtensibleImport<T> import) =>
            new(import.Provider, import.Version, SerializeToElement(import.Config));

        /// <summary>Deserialize a resource from <see cref="JsonElement"/> properties to a concrete type.</summary>
        public static ExtensibleResource<T> MapToConcrete<T>(ExtensibleResource<JsonElement> resource) =>
            new(resource.Type, Deserialize<T>(resource.Properties));

        /// <summary>Serialize a resource from a concrete type to <see cref="JsonElement"/> properties.</summary>
        public static ExtensibleResource<JsonElement> MapToGeneric<T>(ExtensibleResource<T> resource) =>
            new(resource.Type, SerializeToElement(resource.Properties));

        private static T Deserialize<T>(JsonElement element) =>
            ExtensibilityJsonSerializer.Default.Deserialize<T>(element) ??
            throw new InvalidOperationException($"Could not deserialize JSON element to a {nameof(T)}.");

        private static JsonElement SerializeToElement<T>(T value) => ExtensibilityJsonSerializer.Default.SerializeToElement(value);
    }
}
