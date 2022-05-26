using Azure.ResourceManager.Extensibility.Core.Extensions;
using Json.More;
using Json.Pointer;
using Json.Schema;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Azure.ResourceManager.Extensibility.Core.Validators
{
    public class ExtensibleResourceValidator
    {
        private readonly JsonSchema typeSchema;

        private readonly Func<string, JsonSchema> propertiesSchemaSelector;

        public ExtensibleResourceValidator(Regex typeRegex, Func<string, JsonSchema> propertiesSchemaSelector)
        {
            this.typeSchema = new JsonSchemaBuilder().Pattern(typeRegex);
            this.propertiesSchemaSelector = propertiesSchemaSelector;
        }

        public ExtensibleResourceValidator(Regex typeRegex, JsonSchema propertiesSchema)
            : this(typeRegex, _ => propertiesSchema)
        {
        }

        public IEnumerable<ExtensibilityError> Validate(ExtensibleResource<JsonElement> resource)
        {
            // Validate resource type.
            var typeErrors = Validate(resource.GetJsonPointer(x => x.Type), this.typeSchema, resource.Type.AsJsonElement());

            if (typeErrors.Any())
            {
                // Stop validating properties if the resource type is invalid.
                return typeErrors;
            }

            // Validate resource properties.
            var propertiesSchema = this.propertiesSchemaSelector(resource.Type);
            var propertiesErrors = Validate(resource.GetJsonPointer(x => x.Properties), propertiesSchema, resource.Properties);

            return propertiesErrors;
        }

        private static IEnumerable<ExtensibilityError> Validate(JsonPointer basePointer, JsonSchema schema, JsonElement value)
        {
            var violations = JsonSchemaValidator.Validate(schema, value);

            if (violations.Any())
            {
                foreach (var violation in violations)
                {
                    // Prepend "/resources/{resource.SymbolicName}" to target.
                    var target = basePointer.Combine(violation.Target);
                    var error = (violation with { Target = target }).ToExtensibilityError();

                    yield return error;
                }
            }
        }
    }
}
