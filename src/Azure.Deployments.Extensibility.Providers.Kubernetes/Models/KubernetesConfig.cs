// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Json.Schema;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.Models
{
    public record KubernetesConfig(string Namespace, byte[] Kubeconfig, string? Context)
    {
        public readonly static JsonSchema Schema = new JsonSchemaBuilder()
            .Properties(
                ("namespace", new JsonSchemaBuilder().Type(SchemaValueType.String)),
                ("kubeconfig", new JsonSchemaBuilder().Type(SchemaValueType.String)),
                ("context", new JsonSchemaBuilder().Type(SchemaValueType.String, SchemaValueType.Null)))
            .AdditionalProperties(false);
    }
}
