using Json.More;
using Json.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Azure.ResourceManager.Extensibility.Providers.Kubernetes.Models
{
    public record KubernetesResourceMetadata(string Name, string? Namespace);

    public record KubernetesResourceProperties
    {
        public readonly static JsonSchema Schema = new JsonSchemaBuilder()
            .Properties(
                ("metadata", new JsonSchemaBuilder()
                    .Properties(
                        ("name", new JsonSchemaBuilder().Type(SchemaValueType.String)),
                        ("namespace", new JsonSchemaBuilder().Type(SchemaValueType.String)))
                    .Required("name")))
            .Required("metadata")
            .AdditionalProperties(true);

        public KubernetesResourceProperties(KubernetesResourceMetadata metadata, Dictionary<string, JsonElement> extensionData)
        {
            this.Metadata = metadata;
            this.AdditionalData = extensionData;
        }

        public KubernetesResourceMetadata Metadata { get; init; }

        // Ideally this should be an immutable dictionary, but it's not supported yet.
        // See: https://github.com/dotnet/runtime/issues/31645.
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? AdditionalData { get; private set; }

        public KubernetesResourceProperties PatchProperty(string propertyName, JsonElementProxy value)
        {
            this.AdditionalData ??= new Dictionary<string, JsonElement>();

            this.AdditionalData[propertyName] = value;

            return this;
        }

        public string ToJsonString()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
            });
        }
    }
}
