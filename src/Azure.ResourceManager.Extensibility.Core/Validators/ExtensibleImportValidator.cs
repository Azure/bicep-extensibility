using Json.Schema;
using System.Text.Json;

namespace Azure.ResourceManager.Extensibility.Core.Validators
{
    public class ExtensibleImportValidator : ExtensibilityValidator<ExtensibleImport<JsonElement>>
    {
        public ExtensibleImportValidator(JsonSchema configSchema)
        {
            this.RuleFor(x => x.Config).MustConformTo(configSchema);
        }
    }
}
