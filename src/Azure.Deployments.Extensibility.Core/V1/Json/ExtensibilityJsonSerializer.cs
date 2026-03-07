// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Azure.Deployments.Extensibility.Core.Json
{
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

        public static readonly JsonSerializerProxy Default = new(new JsonSerializerOptions(WithoutConverters.Options)
        {
            Converters = { new ExtensibilityOperationResponseConverter() },
        });

        public class JsonSerializerProxy
        {
            public JsonSerializerProxy(JsonSerializerOptions options)
            {
                this.Options = options;
            }

            public JsonSerializerOptions Options { get; }

            public TValue? Deserialize<TValue>(string json) => JsonSerializer.Deserialize<TValue>(json, this.Options);

            public TValue? Deserialize<TValue>(Stream json) => JsonSerializer.Deserialize<TValue>(json, this.Options);

            public TValue? Deserialize<TValue>(JsonElement element) => JsonSerializer.Deserialize<TValue>(element, this.Options);

            public string Serialize<TValue>(TValue value) => JsonSerializer.Serialize(value, this.Options);

            public JsonElement SerializeToElement<TValue>(TValue value) => JsonSerializer.SerializeToElement(value, this.Options);

            public void Serialize<TValue>(Utf8JsonWriter writer, TValue value) => JsonSerializer.Serialize(writer, value, this.Options);
        }
    }
}
