using FluentValidation;
using Json.Schema;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Azure.ResourceManager.Extensibility.Core.Validators
{
    public class ExtensibleResourceValidator : ExtensibilityValidator<ExtensibleResource<JsonElement>>
    {
        public ExtensibleResourceValidator(Regex typeRegex, JsonSchema propertySchema)
        {
            this.RuleFor(x => x.Type).Matches(typeRegex);
            this.RuleFor(x => x.Properties).MustConformTo(propertySchema);
        }
    }
}
