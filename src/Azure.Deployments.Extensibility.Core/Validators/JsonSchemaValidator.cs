using Json.Pointer;
using Json.Schema;
using System.Text.Json;

namespace Azure.Deployments.Extensibility.Core.Validators
{
    public record JsonSchemaViolation(JsonPointer Target, string ErrorMessage);

    public static class JsonSchemaValidator
    {
        private readonly static ValidationOptions JsonSchemaValidationOptions = new()
        {
            // OutputFormat.Basic indicates that all nodes will be listed as children of the top node.
            OutputFormat = OutputFormat.Basic,
            ValidateAs = Draft.Draft7
        };

        static JsonSchemaValidator()
        {
            // TODO:
            // - Customize other error messages
            // - Localization.
            ErrorMessages.Pattern = @$"Value does not match the regular expression [[pattern]].";
            ErrorMessages.FalseSchema = @$"Value fails against the [[schema]] schema."; 
        }

        public static IEnumerable<JsonSchemaViolation> Validate(JsonSchema schema, JsonElement element)
        {
            var rootResult = schema.Validate(element, JsonSchemaValidationOptions);

            if (rootResult.IsValid)
            {
                yield break;
            }

            var invalidResults = rootResult.NestedResults.Count == 0 ? new[] { rootResult } : rootResult.NestedResults;

            foreach (var result in invalidResults)
            {
                // result.Message must be non-null. This is just to make the type system happy.
                var errorMessage = result.Message ?? "Value is invalid.";

                if (errorMessage.Equals(ErrorMessages.FalseSchema))
                {
                    errorMessage = errorMessage.Replace("[[schema]]", $@"""{result.SchemaLocation}"": false");
                }

                if (!errorMessage.EndsWith('.'))
                {
                    // The default error message does not end with a period.
                    errorMessage = $"{errorMessage}.";
                }

                yield return new JsonSchemaViolation(result.InstanceLocation, errorMessage);
            }
        }
    }
}
