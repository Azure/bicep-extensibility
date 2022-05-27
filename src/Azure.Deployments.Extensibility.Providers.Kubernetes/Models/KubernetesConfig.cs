using Json.Schema;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.Models
{
    public record KubernetesConfig(string Namespace, byte[] KubeConfig, string? Context)
    {
        public readonly static JsonSchema Schema = new JsonSchemaBuilder()
            .Properties(
                ("kubeConfig", new JsonSchemaBuilder().Type(SchemaValueType.String)),
                ("context", new JsonSchemaBuilder().Type(SchemaValueType.String)))
            .AdditionalProperties(false);
    }
}
