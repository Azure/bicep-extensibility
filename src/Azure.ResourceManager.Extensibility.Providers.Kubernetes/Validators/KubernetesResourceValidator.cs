using Azure.ResourceManager.Extensibility.Core;
using Azure.ResourceManager.Extensibility.Core.Validators;
using Json.Schema;

namespace Azure.ResourceManager.Extensibility.Providers.Kubernetes.Validators
{
    public class KubernetesResourceValidator : ExtensibilityValidator<ExtensibleResource>
    {
        private static readonly JsonSchema KubernetesResourcePropertiesSchema = new JsonSchemaBuilder()
            .Properties(
                ("metadata", new JsonSchemaBuilder()
                    .Properties(
                        ("name", new JsonSchemaBuilder().Type(SchemaValueType.String)),
                        ("namespace", new JsonSchemaBuilder().Type(SchemaValueType.String)))
                    .Required("name")))
            .Required("metadata")
            .AdditionalProperties(true);

        public KubernetesResourceValidator()
        {
            this.RuleFor(x => x.Properties).MustConformTo(KubernetesResourcePropertiesSchema);
        }
    }
}
