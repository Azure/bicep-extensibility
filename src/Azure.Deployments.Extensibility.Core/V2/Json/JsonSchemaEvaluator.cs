// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Json.Pointer;
using Json.Schema;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Core.V2.Json
{
    public record JsonSchemaViolation(JsonPointer InstanceLocation, string ErrorMessage);

    public class JsonSchemaEvaluator
    {
        private readonly JsonSchema schema;
        private readonly EvaluationOptions evaluationOptions;

        static JsonSchemaEvaluator()
        {
            ErrorMessages.Pattern = @$"Value does not match the regular expression [[pattern]].";
            ErrorMessages.FalseSchema = @$"Value fails against the [[schema]] schema.";
        }

        public JsonSchemaEvaluator(JsonSchema schema, SpecVersion specVersion = SpecVersion.Draft7)
        {
            this.schema = schema;
            this.evaluationOptions = new()
            {
                OutputFormat = OutputFormat.List,
                EvaluateAs = specVersion
            };
        }

        public IEnumerable<JsonSchemaViolation> Evaluate(JsonElement value)
        {
            var result = this.schema.Evaluate(value, this.evaluationOptions);

            return CheckResult(result);
        }

        public IEnumerable<JsonSchemaViolation> Evaluate(JsonNode? value)
        {
            var result = this.schema.Evaluate(value, this.evaluationOptions);

            return CheckResult(result);
        }

        private static IEnumerable<JsonSchemaViolation> CheckResult(EvaluationResults result)
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
