// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using System.Linq.Expressions;

namespace Azure.Deployments.Extensibility.Core.V2.Validation
{
    /// <summary>
    /// Abstract base class for building fluent model validators.
    /// Subclasses define validation rules using <see cref="Ensure{TProperty}"/> in their constructors.
    /// Rules can declare dependencies on other rules using
    /// <see cref="IPropertyRuleBuilder{TModel,TProperty}.DependsOn"/>, causing them to be skipped
    /// if any dependency rule produced errors.
    /// </summary>
    /// <typeparam name="TModel">The type of model to validate.</typeparam>
    public abstract class ModelValidator<TModel> : IModelValidator<TModel>
        where TModel : class
    {
        private readonly List<(IModelValidationRule<TModel> Rule, IPropertyRuleBuilderInternal Builder)> rules = [];

        public IPropertyRuleBuilder<TModel, TProperty> Ensure<TProperty>(Expression<Func<TModel, TProperty>> propertyExpression)
        {
            var rule = new PropertyRule<TModel, TProperty>(propertyExpression);
            var builder = new PropertyRuleBuilder<TModel, TProperty>(rule);

            this.rules.Add((rule, builder));

            return builder;
        }

        public Error? Validate(TModel model) => AggregateErrorDetails(this.ValidateRules(model).ToArray());

        private IEnumerable<ErrorDetail> ValidateRules(TModel model)
        {
            var failedBuilders = new HashSet<IPropertyRuleBuilder>();

            foreach (var (rule, builder) in this.rules)
            {
                if (builder.Dependencies.Any(dep => failedBuilders.Contains(dep)))
                {
                    continue;
                }

                var hadErrors = false;

                foreach (var errorDetail in rule.Validate(model))
                {
                    hadErrors = true;
                    yield return errorDetail;
                }

                if (hadErrors)
                {
                    failedBuilders.Add(builder);
                }
            }
        }

        private static Error? AggregateErrorDetails(ErrorDetail[] errorDetails) => errorDetails.Length switch
        {
            0 => null,
            1 => errorDetails[0].ToError(),
            _ => new Error
            {
                Code = "MultipleErrorsOccurred",
                Message = "Multiple errors occurred. Please refer to details for more information.",
                Details = errorDetails,
            },
        };
    }
}
