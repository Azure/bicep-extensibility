// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using Azure.Deployments.Extensibility.Core.V2.Json;
using Json.Pointer;
using Json.Schema;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Core.V2.Validation.Criteria
{
    /// <summary>
    /// Resolves a <see cref="JsonSchema"/> from the model being validated.
    /// </summary>
    public delegate JsonSchema JsonSchemaResolver<TModel>(TModel model);

    /// <summary>
    /// A validation criterion that fails when a JSON property does not conform to the specified JSON schema.
    /// </summary>
    public class MatchJsonSchemaCriterion<TModel> : IPropertyRuleCriterion<TModel, JsonNode?>, IPropertyRuleCriterion<TModel, JsonElement>, IConfigurableErrorCriterion
    {
        private readonly JsonSchemaResolver<TModel> schemaResolver;
        private readonly SpecVersion schemaSpecVersion;

        /// <summary>
        /// Initializes a new instance of the <see cref="MatchJsonSchemaCriterion{TModel}"/> class.
        /// </summary>
        /// <param name="schemaResolver">A delegate that resolves the JSON schema from the model.</param>
        /// <param name="schemaSpecVersion">The JSON Schema specification version to use during evaluation.</param>
        public MatchJsonSchemaCriterion(JsonSchemaResolver<TModel> schemaResolver, SpecVersion schemaSpecVersion)
        {
            this.schemaResolver = schemaResolver;
            this.schemaSpecVersion = schemaSpecVersion;
        }

        /// <inheritdoc/>
        public string ErrorCode { get; set; } = "JsonSchemaViolation";

        /// <inheritdoc/>
        public string ErrorMessage { get; set; } = "";

        /// <inheritdoc/>
        public IEnumerable<ErrorDetail> Evaluate(TModel model, JsonNode? propertyValue, JsonPointer propertyPointer) =>
            this.CreateSchemaEvaluator(model).Evaluate(propertyValue).Select(schemaViolation => this.CreateErrorDetail(schemaViolation, propertyPointer));

        /// <inheritdoc/>
        public IEnumerable<ErrorDetail> Evaluate(TModel model, JsonElement propertyValue, JsonPointer propertyPointer) =>
            this.CreateSchemaEvaluator(model).Evaluate(propertyValue).Select(schemaViolation => this.CreateErrorDetail(schemaViolation, propertyPointer));

        private JsonSchemaEvaluator CreateSchemaEvaluator(TModel model) => new(this.schemaResolver.Invoke(model), this.schemaSpecVersion);

        private ErrorDetail CreateErrorDetail(JsonSchemaViolation schemaViolation, JsonPointer propertyPointer) =>
            new(this.ErrorCode, schemaViolation.ErrorMessage, propertyPointer.Combine(schemaViolation.InstanceLocation));
    }
}
