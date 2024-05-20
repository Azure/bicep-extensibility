// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Json;
using Azure.Deployments.Extensibility.Core.V2.Models;
using Json.Pointer;
using Json.Schema;

namespace Azure.Deployments.Extensibility.Core.V2.Validation.Rules
{
    public delegate JsonSchema ResourceIdentifiersSchemaResolver(string type, string? apiVersion);

    public class ResourceIdentifiersMustMatchSchema : ModelValidationRuleContext, IModelValidationRule<ResourceReference>
    {
        private static readonly JsonPointer IdentifiersPointer = JsonPointer.Create("identifiers");

        public JsonSchema? Schema { get; set; }

        public ResourceIdentifiersSchemaResolver? SchemaResolver { get; set; }

        public SpecVersion SchemaSpecVersion { get; set; } = SpecVersion.Draft7;

        public IEnumerable<ErrorDetail> Validate(ResourceReference resourceReference)
        {
            var schema = this.Schema ??
                this.SchemaResolver?.Invoke(resourceReference.Type, resourceReference.ApiVersion) ??
                throw new ArgumentException($"Either {nameof(this.Schema)} or {nameof(this.SchemaResolver)} must be provided and cannot be null.");

            var schemaValidator = new JsonSchemaEvaluator(schema, this.SchemaSpecVersion);
            var schemaViolations = schemaValidator.Evaluate(resourceReference.Identifiers);

            foreach (var (instanceLocation, errorMessage) in schemaViolations)
            {
                yield return new("InvalidIdentifier", errorMessage, IdentifiersPointer.Combine(instanceLocation));
            }
        }
    }
}
