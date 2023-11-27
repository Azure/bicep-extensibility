// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Json.Schema;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Schemas
{
    public static class JsonSchemas
    {
        public readonly static JsonSchema K8sClusterAccessConfig = LoadFromEmbededResource();

        public readonly static JsonSchema K8sResourceProperties = LoadFromEmbededResource();

        private static JsonSchema LoadFromEmbededResource([CallerMemberName] string schemaResourceName = "")
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{typeof(JsonSchemas).Namespace}.{schemaResourceName}.schema.json")
                ?? throw new InvalidOperationException($"Unable to find embeded resource {schemaResourceName}.");

            return JsonSerializer.Deserialize<JsonSchema>(stream)
                ?? throw new InvalidOperationException($"Cannot deserialize embeded resource {schemaResourceName} to JsonSchema.");
        }
    }
}
