// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Json;
using Azure.Deployments.Extensibility.Core.V2.Models;
using Json.Pointer;
using Json.Schema;

namespace Azure.Deployments.Extensibility.Core.V2.Validation.Rules
{
    public delegate JsonSchema ResourcePropertiesSchemaResolver(string type, string? apiVersion);

    public class ResourcePropertiesMustMatchSchema : ModelValidationRuleContext, IModelValidationRule<ResourceSpecification>
    {
        private static readonly JsonPointer PropertiesPointer = JsonPointer.Create("properties");

        public JsonSchema? Schema { get; set; }

        public ResourceIdentifiersSchemaResolver? SchemaResolver { get; set; }

        public SpecVersion SchemaSpecVersion { get; set; } = SpecVersion.Draft7;

        public IEnumerable<ErrorDetail> Validate(ResourceSpecification resourceDefinition)
        {
            var schema = this.Schema ??
                this.SchemaResolver?.Invoke(resourceDefinition.Type, resourceDefinition.ApiVersion) ??
                throw new ArgumentException($"Either {nameof(this.Schema)} or {nameof(this.SchemaResolver)} must be provided and cannot be null.");

            var schemaValidator = new JsonSchemaEvaluator(schema, this.SchemaSpecVersion);
            var schemaViolations = schemaValidator.Evaluate(resourceDefinition.Properties);

            foreach (var (instanceLocation, errorMessage) in schemaViolations)
            {
                yield return new("InvalidProperty", errorMessage, PropertiesPointer.Combine(instanceLocation));
            }
        }
    }
}
