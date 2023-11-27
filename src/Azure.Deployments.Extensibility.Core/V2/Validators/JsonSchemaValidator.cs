// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Json.Pointer;
using Json.Schema;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Core.V2.Validators
{
    public record JsonSchemaViolation(JsonPointer InstanceLocation, string ErrorMessage);

    public class JsonSchemaValidator(JsonSchema schema, SpecVersion specVersion = SpecVersion.Draft7) : IValidator<JsonElement, IReadOnlyList<JsonSchemaViolation>>, IValidator<JsonNode, IReadOnlyList<JsonSchemaViolation>>
    {
        private readonly EvaluationOptions evaluationOptions = new()
        {
            OutputFormat = OutputFormat.List,
            EvaluateAs = specVersion
        };

        static JsonSchemaValidator()
        {
            ErrorMessages.Pattern = @$"Value does not match the regular expression [[pattern]].";
            ErrorMessages.FalseSchema = @$"Value fails against the [[schema]] schema.";
        }

        public IReadOnlyList<JsonSchemaViolation> Validate(JsonElement value)
        {
            var result = schema.Evaluate(value, this.evaluationOptions);

            return CheckResult(result);
        }

        public IReadOnlyList<JsonSchemaViolation> Validate(JsonNode? value)
        {
            var result = schema.Evaluate(value, this.evaluationOptions);

            return CheckResult(result);
        }

        private static IReadOnlyList<JsonSchemaViolation> CheckResult(EvaluationResults result)
        {
            if (result.IsValid)
            {
                return Array.Empty<JsonSchemaViolation>();
            }

            var schemaViolations = new List<JsonSchemaViolation>();

            foreach (var detail in result.Details)
            {
                if (detail.Errors is not null)
                {
                    foreach (var error in detail.Errors)
                    {
                        var errorMessage = error.Value;

                        if (errorMessage.Equals(ErrorMessages.FalseSchema))
                        {
                            errorMessage = errorMessage.Replace("[[schema]]", $@"""{detail.SchemaLocation.Fragment}"": false");
                        }

                        if (!errorMessage.EndsWith('.'))
                        {
                            // The default error message may not end with a period.
                            errorMessage = $"{errorMessage}.";
                        }

                        schemaViolations.Add(new JsonSchemaViolation(detail.InstanceLocation, errorMessage));
                    }
                }
            }

            return schemaViolations;
        }
    }
}