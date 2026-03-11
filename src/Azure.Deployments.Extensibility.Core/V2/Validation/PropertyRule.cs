// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using Json.Pointer;
using System.Linq.Expressions;

namespace Azure.Deployments.Extensibility.Core.V2.Validation
{
    internal class PropertyRule<TModel, TProperty> : IModelValidationRule<TModel>
    {
        private readonly List<IPropertyRuleCriterion<TModel, TProperty>> criteria = [];
        private readonly Func<TModel, TProperty> compiledExpression;
        private readonly JsonPointer propertyPointer;

        public PropertyRule(Expression<Func<TModel, TProperty>> propertyExpression)
        {
            this.compiledExpression = propertyExpression.Compile();
            this.propertyPointer = CreatePropertyPointer(propertyExpression);
        }

        public void AddCriterion(IPropertyRuleCriterion<TModel, TProperty> criterion) => this.criteria.Add(criterion);

        public IEnumerable<ErrorDetail> Validate(TModel model)
        {
            if (this.criteria.Count == 0)
            {
                yield break;
            }

            var property = this.compiledExpression(model);

            foreach (var criterion in this.criteria)
            {
                var criterionSatisfied = true;

                foreach (var errorDetail in criterion.Evaluate(model, property, this.propertyPointer))
                {
                    criterionSatisfied = false;
                    yield return errorDetail;
                }

                if (!criterionSatisfied)
                {
                    // Do not evaluate subsequent criteria if the current one failed.
                    yield break;
                }
            }
        }

        private static JsonPointer CreatePropertyPointer(Expression<Func<TModel, TProperty>> propertyExpression) => JsonPointer.Create(
            Expression.Lambda<Func<TModel, object>>(
                Expression.Convert(propertyExpression.Body, typeof(object)), propertyExpression.Parameters),
            new PointerCreationOptions
            {
                PropertyNameResolver = PropertyNameResolvers.CamelCase,
            });
    }
}
