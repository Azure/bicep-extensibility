// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Validation.Criteria;
using Json.Schema;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Azure.Deployments.Extensibility.Core.V2.Validation
{
    /// <summary>
    /// Extension methods that add common validation criteria to an <see cref="IPropertyRuleBuilder{TModel, TProperty}"/>.
    /// </summary>
    public static class PropertyRuleBuilderExtensions
    {
        public static IPropertyRuleBuilder<TModel, TProperty> NotNull<TModel, TProperty>(
            this IPropertyRuleBuilder<TModel, TProperty> builder,
            Action<ErrorBuilder>? configureError = null)
        {
            var criterion = new NotBeNullCriterion<TModel, TProperty>();

            ApplyErrorOverrides(criterion, configureError);
            builder.AddCriterion(criterion);

            return builder;
        }

        public static IPropertyRuleBuilder<TModel, string?> MatchesRegex<TModel>(
            this IPropertyRuleBuilder<TModel, string?> builder,
            Regex regex,
            Action<ErrorBuilder>? configureError = null)
        {
            var criterion = new MatchRegexCriterion<TModel>(regex);

            ApplyErrorOverrides(criterion, configureError);
            builder.AddCriterion(criterion);

            return builder;
        }

        public static IPropertyRuleBuilder<TModel, TJsonNode?> MatchesJsonSchema<TModel, TJsonNode>(
            this IPropertyRuleBuilder<TModel, TJsonNode?> builder,
            JsonSchema schema,
            SpecVersion schemaSpecVersion = SpecVersion.Draft7,
            Action<ErrorBuilder>? configureError = null)
            where TJsonNode : JsonNode
        {
            var criterion = new MatchJsonSchemaCriterion<TModel>(_ => schema, schemaSpecVersion);

            ApplyErrorOverrides(criterion, configureError);
            builder.AddCriterion(criterion);

            return builder;
        }

        public static IPropertyRuleBuilder<TModel, JsonElement> MatchesJsonSchema<TModel>(
            this IPropertyRuleBuilder<TModel, JsonElement> builder,
            JsonSchemaResolver<TModel> schemaResolver,
            SpecVersion schemaSpecVersion = SpecVersion.Draft7,
            Action<ErrorBuilder>? configureError = null)
        {
            var criterion = new MatchJsonSchemaCriterion<TModel>(schemaResolver, schemaSpecVersion);

            ApplyErrorOverrides(criterion, configureError);
            builder.AddCriterion(criterion);

            return builder;
        }

        public static IPropertyRuleBuilder<TModel, TProperty> Satisfies<TModel, TProperty>(
            this IPropertyRuleBuilder<TModel, TProperty> builder,
            Func<TProperty, bool> predicate,
            Action<ErrorBuilder>? configureError = null)
        {
            var criterion = new SatisfiesCriterion<TModel, TProperty>(predicate);

            ApplyErrorOverrides(criterion, configureError);
            builder.AddCriterion(criterion);

            return builder;
        }

        private static void ApplyErrorOverrides(IConfigurableErrorCriterion criterion, Action<ErrorBuilder>? configureError)
        {
            if (configureError is null)
            {
                return;
            }

            var errorBuilder = new ErrorBuilder();
            configureError(errorBuilder);

            if (errorBuilder.Code is not null)
            {
                criterion.ErrorCode = errorBuilder.Code;
            }

            if (errorBuilder.Message is not null)
            {
                criterion.ErrorMessage = errorBuilder.Message;
            }
        }
    }
}
