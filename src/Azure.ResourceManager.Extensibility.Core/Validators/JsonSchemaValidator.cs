using FluentValidation;
using FluentValidation.Results;
using Json.Schema;
using System.Text.Json;

namespace Azure.ResourceManager.Extensibility.Core.Validators
{
    public static class JsonSchemaValidator
    {
        private const string AdditionalPropertiesSchemaLocationSuffix = "/additionalProperties";

        private const string RegexSchemaLocationSuffix = "/pattern";

        private readonly static ValidationOptions JsonSchemaValidationOptions = new()
        {
            // OutputFormat.Basic indicates that all nodes will be listed as children of the top node.
            OutputFormat = OutputFormat.Basic,
            ValidateAs = Draft.Draft7
        };

        public static IRuleBuilderOptionsConditions<T, JsonElement> MustConformTo<T>(this IRuleBuilder<T, JsonElement> builder, JsonSchema schema)
        {
            return builder.Custom((element, context) =>
            {
                var rootResult = schema.Validate(element, JsonSchemaValidationOptions);

                if (rootResult.IsValid)
                {
                    return;
                }

                var invalidResults = rootResult.NestedResults.Count == 0 ? new[] { rootResult } : rootResult.NestedResults;
                var propertyChain = context.PropertyChain.BuildPropertyName(context.PropertyName);
                var basePointer = PropertyChainConverter.ConvertToJsonPointer(propertyChain);

                foreach (var result in invalidResults)
                {
                    // result.Message must be non-null. This is just to make the type system happy.
                    var errorMessage = result.Message ?? "The property is invalid.";

                    if (!errorMessage.EndsWith('.'))
                    {
                        // The default error message may not end with a period.
                        errorMessage = $"{errorMessage}.";
                    }

                    if (IsAdditionalPropertyError(result))
                    {
                        errorMessage = "The property is not allowed.";
                    }

                    if (IsRegexError(result))
                    {
                        var schemaElement = JsonSerializer.SerializeToElement(schema);
                        var regex = result.SchemaLocation.Evaluate(schemaElement);
                        errorMessage = @$"Value does not match the pattern of ""{regex}"".";
                    }

                    context.AddFailure(new ValidationFailure(basePointer.Combine(result.InstanceLocation).ToString(), errorMessage)
                    {
                        ErrorCode = "JsonSchemaViolation"
                    });
                }
            });
        }

        private static bool IsAdditionalPropertyError(ValidationResults result) =>
            result.SchemaLocation.Source.EndsWith(AdditionalPropertiesSchemaLocationSuffix);

        private static bool IsRegexError(ValidationResults result) =>
            result.SchemaLocation.Source.EndsWith(RegexSchemaLocationSuffix);
    }
}
