// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Json.Schema;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Azure.Deployments.Extensibility.Extensions.Kubernetes.Validation.Schemas
{
    [JsonSerializable(typeof(JsonSchema))]
    internal partial class JsonSchemaSerializerContext : JsonSerializerContext
    {
    }

    internal static class JsonSchemas
    {
        public readonly static JsonSchema K8sResourceConfig = LoadFromEmbededResource();

        public readonly static JsonSchema K8sResourceProperties = LoadFromEmbededResource();

        public readonly static JsonSchema K8sResourceIdentifiers = LoadFromEmbededResource();

        private static JsonSchema LoadFromEmbededResource([CallerMemberName] string schemaResourceName = "")
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{typeof(JsonSchemas).Namespace}.{schemaResourceName}.schema.json") ??
                throw new InvalidOperationException($"Cannot find embeded resource {schemaResourceName}.");

            return JsonSerializer.Deserialize(stream, JsonSchemaSerializerContext.Default.JsonSchema) ??
                throw new InvalidOperationException($"Cannot deserialize embeded resource {schemaResourceName} to JsonSchema.");
        }
    }
}
