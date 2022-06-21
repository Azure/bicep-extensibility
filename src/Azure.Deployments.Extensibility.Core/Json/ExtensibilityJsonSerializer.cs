using System.Text.Json;

namespace Azure.Deployments.Extensibility.Core.Json
{
    public class ExtensibilityJsonSerializer
    {
        public static readonly JsonSerializerProxy Default = new(new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        });

        public class JsonSerializerProxy
        {
            private readonly JsonSerializerOptions options;

            public JsonSerializerProxy(JsonSerializerOptions options)
            {
                this.options = options;
            }

            public TValue? Deserialize<TValue>(string json) => JsonSerializer.Deserialize<TValue>(json, this.options);

            public TValue? Deserialize<TValue>(JsonElement element) => JsonSerializer.Deserialize<TValue>(element, this.options);

            public string Serialize<TValue>(TValue value) => JsonSerializer.Serialize(value, this.options);

            public JsonElement SerializeToElement<TValue>(TValue value) => JsonSerializer.SerializeToElement(value, this.options);
        }
    }
}
