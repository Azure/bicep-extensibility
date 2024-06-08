// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Models;
using System.Linq.Expressions;

namespace Azure.Deployments.Extensibility.Core.V2.Validation
{
    public abstract class ModelValidator<TModel> : IModelValidator<TModel>
        where TModel : class
    {
        private readonly List<IModelValidationRule<TModel>> rules = [];

        private readonly HashSet<IModelValidationRule<TModel>> dependentRules = [];

        public IPropertyRuleBuilder<TModel, TProperty> AnyValid<TProperty>(Expression<Func<TModel,  TProperty>> propertyExpression)
        {
            var rule = new PropertyRule<TModel, TProperty>(propertyExpression);

            this.rules.Add(rule);

            return new PropertyRuleBuilder<TModel, TProperty>(rule);
        }

        public IPropertyRuleBuilder<TModel, TProperty> WhenPrecedingRulesSatisfied<TProperty>(Expression<Func<TModel,  TProperty>> propertyExpression)
        {
            var rule = new PropertyRule<TModel, TProperty>(propertyExpression);

            this.rules.Add(rule);
            this.dependentRules.Add(rule);

            return new PropertyRuleBuilder<TModel, TProperty>(rule);
        }

        public Error? Validate(TModel model) => AggregateErrorDetails(this.ValidateRules(model).ToArray());

        private IEnumerable<ErrorDetail> ValidateRules(TModel model)
        {
            var dependencyRulesSatisfied = true;

            foreach (var rule in this.rules)
            {
                if (this.dependentRules.Contains(rule) && !dependencyRulesSatisfied)
                {
                    continue;
                }

                foreach (var errorDetail in rule.Validate(model))
                {
                    dependencyRulesSatisfied = false;
                    yield return errorDetail;
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
