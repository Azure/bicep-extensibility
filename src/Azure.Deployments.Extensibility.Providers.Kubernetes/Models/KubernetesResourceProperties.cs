// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
            // Adding the default constructor to make deserialization work.
        }

        public KubernetesResourceProperties(KubernetesResourceMetadata metadata, Dictionary<string, JsonElement>? additionalData)
        {
            this.Metadata = metadata;
            this.AdditionalData = additionalData;
        }

        public KubernetesResourceMetadata Metadata { get; set; }

        // Ideally this should be an immutable dictionary, but it's not supported yet.
        // See: https://github.com/dotnet/runtime/issues/31645.
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? AdditionalData { get; set; }

        public KubernetesResourceProperties PatchProperty(string propertyName, JsonElementProxy value)
        {
            if (this.AdditionalData is null)
            {
                return new(this.Metadata, new Dictionary<string, JsonElement>
                {
                    [propertyName] = value,
                });
            }

            var updatedData = this.AdditionalData.ToDictionary(x => x.Key, x => x.Value);

            updatedData[propertyName] = value;

            return new(this.Metadata, updatedData);
        }
    }
}
