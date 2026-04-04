// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Azure.Deployments.Extensibility.Core.Json
{
    /// <summary>
    /// Provides pre-configured JSON serializer options and a proxy for V1 extensibility model serialization.
    /// </summary>
    public class ExtensibilityJsonSerializer
    {
        internal static readonly JsonSerializerProxy WithoutConverters = new(new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,

            // We are not emitting JSON into an HTML page, so it's safe to use UnsafeRelaxedJsonEscaping.
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        });

        /// <summary>
        /// The default serializer proxy, configured with camelCase naming and the
        /// <see cref="ExtensibilityOperationResponseConverter"/>.
        /// </summary>
        public static readonly JsonSerializerProxy Default = new(new JsonSerializerOptions(WithoutConverters.Options)
        {
            Converters = { new ExtensibilityOperationResponseConverter() },
        });

        /// <summary>
        /// Wraps a <see cref="JsonSerializerOptions"/> instance with convenience methods
        /// for serialization and deserialization.
        /// </summary>
        public class JsonSerializerProxy
        {
            /// <summary>
            /// Initializes a new instance with the specified options.
            /// </summary>
            /// <param name="options">The JSON serializer options to use.</param>
            public JsonSerializerProxy(JsonSerializerOptions options)
            {
                this.Options = options;
            }

            /// <summary>Gets the underlying <see cref="JsonSerializerOptions"/>.</summary>
            public JsonSerializerOptions Options { get; }

            /// <summary>Deserialize a JSON string to the specified type.</summary>
            public TValue? Deserialize<TValue>(string json) => JsonSerializer.Deserialize<TValue>(json, this.Options);

            /// <summary>Deserialize a JSON stream to the specified type.</summary>
            public TValue? Deserialize<TValue>(Stream json) => JsonSerializer.Deserialize<TValue>(json, this.Options);

            /// <summary>Deserialize a <see cref="JsonElement"/> to the specified type.</summary>
            public TValue? Deserialize<TValue>(JsonElement element) => JsonSerializer.Deserialize<TValue>(element, this.Options);

            /// <summary>Serialize the specified value to a JSON string.</summary>
            public string Serialize<TValue>(TValue value) => JsonSerializer.Serialize(value, this.Options);

            /// <summary>Serialize the specified value to a <see cref="JsonElement"/>.</summary>
            public JsonElement SerializeToElement<TValue>(TValue value) => JsonSerializer.SerializeToElement(value, this.Options);

            /// <summary>Serialize the specified value to a <see cref="Utf8JsonWriter"/>.</summary>
            public void Serialize<TValue>(Utf8JsonWriter writer, TValue value) => JsonSerializer.Serialize(writer, value, this.Options);
        }
    }
}
