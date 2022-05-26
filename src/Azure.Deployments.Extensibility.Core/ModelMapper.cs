﻿using System.Text.Json;

namespace Azure.Deployments.Extensibility.Core
{
    public static class ModelMapper
    {
        private readonly static JsonSerializerOptions JsonSerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        public static ExtensibleImport<T> MapToConcrete<T>(ExtensibleImport<JsonElement> import) =>
            new(import.SymbolicName, import.Provider, import.Version, Deserialize<T>(import.Config));

        public static ExtensibleImport<JsonElement> MapToGeneric<T>(ExtensibleImport<T> import) =>
            new(import.SymbolicName, import.Provider, import.Version, SerializeToElement(import.Config));

        public static ExtensibleResource<T> MapToConcrete<T>(ExtensibleResource<JsonElement> resource) =>
            new(resource.SymbolicName, resource.Type, Deserialize<T>(resource.Properties));

        public static ExtensibleResource<JsonElement> MapToGeneric<T>(ExtensibleResource<T> resource) =>
            new(resource.SymbolicName, resource.Type, SerializeToElement(resource.Properties));

        private static T Deserialize<T>(JsonElement element) =>
            JsonSerializer.Deserialize<T>(element, JsonSerializerOptions) ??
            throw new InvalidOperationException($"Could not deserialize JSON element to a {nameof(T)}.");

        private static JsonElement SerializeToElement<T>(T value) =>
            JsonSerializer.SerializeToElement(value, JsonSerializerOptions);
    }
}
