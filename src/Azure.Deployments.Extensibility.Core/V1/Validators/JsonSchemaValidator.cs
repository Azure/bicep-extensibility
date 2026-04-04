// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Json.Pointer;
using Json.Schema;
using System.Text.Json;

namespace Azure.Deployments.Extensibility.Core.Validators
{
    /// <summary>
    /// Represents a single JSON Schema validation violation.
    /// </summary>
    /// <param name="Target">The JSON Pointer to the location that caused the violation.</param>
    /// <param name="ErrorMessage">A human-readable description of the violation.</param>
    public record JsonSchemaViolation(JsonPointer Target, string ErrorMessage);

    /// <summary>
    /// Validates a <see cref="JsonElement"/> against a JSON Schema (Draft 7).
    /// </summary>
    public static class JsonSchemaValidator
    {
        private readonly static EvaluationOptions JsonSchemaEvaluationOptions = new()
        {
            // OutputFormat.Basic indicates that all nodes will be listed as children of the top node.
            OutputFormat = OutputFormat.List,
            EvaluateAs = SpecVersion.Draft7
        };

        static JsonSchemaValidator()
        {
            // TODO:
            // - Customize other error messages
            // - Localization.
            ErrorMessages.Pattern = @$"Value does not match the regular expression [[pattern]].";
            ErrorMessages.FalseSchema = @$"Value fails against the [[schema]] schema.";
        }

        /// <summary>
        /// Validate the specified <see cref="JsonElement"/> against the given JSON Schema.
        /// </summary>
        /// <param name="schema">The JSON Schema to validate against.</param>
        /// <param name="element">The JSON element to validate.</param>
        /// <returns>An enumerable of <see cref="JsonSchemaViolation"/> instances for each violation found.</returns>
        public static IEnumerable<JsonSchemaViolation> Validate(JsonSchema schema, JsonElement element)
        {
            var rootResult = schema.Evaluate(element, JsonSchemaEvaluationOptions);

            if (rootResult.IsValid)
            {
                yield break;
            }

            var invalidResults = rootResult.HasDetails ? rootResult.Details : new[] { rootResult };

            foreach (var result in invalidResults)
            {
                if (result.Errors is not null)
                {
                    foreach (var error in result.Errors)
                    {
                        var errorMessage = error.Value;

                        // result.Message must be non-null. This is just to make the type system happy.
                        if (errorMessage.Equals(ErrorMessages.FalseSchema))
                        {
                            errorMessage = errorMessage.Replace("[[schema]]", $@"""{result.SchemaLocation.Fragment}"": false");
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
    }
}
