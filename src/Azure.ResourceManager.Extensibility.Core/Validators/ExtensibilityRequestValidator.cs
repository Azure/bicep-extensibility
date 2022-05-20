using Json.Schema;
using System.Text.RegularExpressions;

namespace Azure.ResourceManager.Extensibility.Core.Validators
{
    public class ExtensibilityRequestValidator : ExtensibilityValidator<ExtensibilityRequest>
    {
        public ExtensibilityRequestValidator(JsonSchema importConfigSchema, Regex resourceTypeRegex, JsonSchema resourcePropertySchema)
        {
            this.RuleFor(x => x.Import).SetValidator(new ExtensibleImportValidator(importConfigSchema));
            this.RuleFor(x => x.Resource).SetValidator(new ExtensibleResourceValidator(resourceTypeRegex, resourcePropertySchema));
        }
    }
}
