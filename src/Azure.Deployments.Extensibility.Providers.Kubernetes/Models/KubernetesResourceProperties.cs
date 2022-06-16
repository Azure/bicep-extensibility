using Json.More;
using Json.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.Models
{
    public readonly record struct KubernetesResourceMetadata(string Name, string? Namespace);

    public record KubernetesResourceProperties
    {
        public readonly static JsonSchema Schema = new JsonSchemaBuilder()
            .Properties(
                ("metadata", new JsonSchemaBuilder()
                    .Properties(
                        ("name", new JsonSchemaBuilder().Type(SchemaValueType.String)),
                        ("namespace", new JsonSchemaBuilder().Type(SchemaValueType.String, SchemaValueType.Null)))
                    .Required("name")))
            .Required("metadata")
            .AdditionalProperties(true);

        public KubernetesResourceProperties()
        {
        }

        public KubernetesResourceProperties(KubernetesResourceMetadata metadata)
            : this(metadata, null)
        {
        }

        public KubernetesResourceProperties(KubernetesResourceMetadata metadata, Dictionary<string, JsonElement>? additionalData)
        {
            this.Metadata = metadata;
            this.AdditionalData = additionalData;
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
    }
}
