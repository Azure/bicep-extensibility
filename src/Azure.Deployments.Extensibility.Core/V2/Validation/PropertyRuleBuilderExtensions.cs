// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Validation.Criteria;
using Azure.Deployments.Extensibility.Core.V2.Validation.Rules;
using Json.Schema;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Azure.Deployments.Extensibility.Core.V2.Validation
{
    public static class PropertyRuleBuilderExtensions
    {
        public static PropertyRuleCriterionBuilder<TModel, string?> MustMatchRegex<TModel>(this IPropertyRuleBuilder<TModel, string?> builder, Regex regex)
        {
            var criterion = new MatchRegexCriterion<TModel>(regex);

            builder.AddCriterion(criterion);

            return new(builder, criterion);
        }

        public static PropertyRuleCriterionBuilder<TModel, TProperty> MustNotBeNull<TModel, TProperty>(this IPropertyRuleBuilder<TModel, TProperty> builder)
        {
            var criterion = new NotBeNullCriterion<TModel, TProperty>();

            builder.AddCriterion(criterion);

            return new(builder, criterion);
        }

        public static PropertyRuleCriterionBuilder<TModel, TJsonNode?> MustMatchJsonSchema<TModel, TJsonNode>(
            this IPropertyRuleBuilder<TModel, TJsonNode?> builder,
            JsonSchema schema,
            SpecVersion schemaSpecVersion = SpecVersion.Draft7)
            where TJsonNode : JsonNode
        {
            var criterion = new MatchJsonSchemaCriterion<TModel>(_ => schema, schemaSpecVersion);

            builder.AddCriterion(criterion);

            return new(builder, criterion);
        }

        public static PropertyRuleCriterionBuilder<TModel, JsonElement> MustMatchJsonSchema<TModel>(
            this IPropertyRuleBuilder<TModel, JsonElement> builder,
            JsonSchemaResolver<TModel> schemaResolver,
            SpecVersion schemaSpecVersion = SpecVersion.Draft7)
        {
            var criterion = new MatchJsonSchemaCriterion<TModel>(schemaResolver, schemaSpecVersion);

            builder.AddCriterion(criterion);

            return new(builder, criterion);
        }
    }
}
