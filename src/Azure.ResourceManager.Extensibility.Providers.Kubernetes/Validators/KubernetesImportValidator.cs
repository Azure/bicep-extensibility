using Azure.ResourceManager.Extensibility.Core;
using Azure.ResourceManager.Extensibility.Core.Validators;
using Json.Schema;

namespace Azure.ResourceManager.Extensibility.Providers.Kubernetes.Validators
{
    public class KubernetesImportValidator : ExtensibilityValidator<ExtensibleImport>
    {
        private static readonly JsonSchema KubernetesConfigSchema = new JsonSchemaBuilder()
            .Properties(
                ("kubeConfig", new JsonSchemaBuilder().Type(SchemaValueType.String)),
                ("context", new JsonSchemaBuilder().Type(SchemaValueType.String)))
            .AdditionalProperties(false);

        public KubernetesImportValidator()
        {
            this.RuleFor(x => x.Config).MustConformTo(KubernetesConfigSchema);
        }
    }
}
