// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Json.More;
using Json.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.Models
{
    public record KubernetesResourceProperties(KubernetesResourceMetadata Metadata)
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

        // Ideally this should be an immutable dictionary, but it's not supported yet.
        // See: https://github.com/dotnet/runtime/issues/31645 for details.
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? AdditionalData { get; init; }

        public KubernetesResourceProperties PatchProperty(string propertyName, JsonElementProxy value)
        {
            if (this.AdditionalData is null)
            {
                return new(this.Metadata)
                {
                    AdditionalData = new Dictionary<string, JsonElement>
                    {
                        [propertyName] = value,
                    },
                };
            }

            var updatedData = this.AdditionalData.ToDictionary(x => x.Key, x => x.Value);

            updatedData[propertyName] = value;

            return new(this.Metadata)
            {
                AdditionalData = updatedData
            };
        }
    }
}
