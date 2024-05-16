// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Json;
using Azure.Deployments.Extensibility.Core.V2.Models;
using Json.Pointer;
using Json.Schema;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Core.V2.Validation.Rules
{
    public class ResourceConfigMustMatchSchema : ModelValidationRuleContext, IModelValidationRule<ResourceSpecification>, IModelValidationRule<ResourceReference>
    {
        private static readonly JsonPointer PropertiesPointer = JsonPointer.Create("config");

        public JsonSchema? Schema { get; set; }

        public SpecVersion SchemaSpecVersion { get; set; } = SpecVersion.Draft7;

        public IEnumerable<ErrorDetail> Validate(ResourceSpecification resourceDefinition) => this.ValidateConfig(resourceDefinition.Config);

        public IEnumerable<ErrorDetail> Validate(ResourceReference resourceReference) => this.ValidateConfig(resourceReference.Config);

        private IEnumerable<ErrorDetail> ValidateConfig(JsonObject? config)
        {
            var schema = this.Schema ?? throw new ArgumentException($"{nameof(this.Schema)} must be provided and cannot be null.");
            var schemaValidator = new JsonSchemaEvaluator(schema, this.SchemaSpecVersion);
            var schemaViolations = schemaValidator.Evaluate(config);

            foreach (var (instanceLocation, errorMessage) in schemaViolations)
            {
                yield return new("InvalidConfig", errorMessage, PropertiesPointer.Combine(instanceLocation));
            }
        }
    }
}
