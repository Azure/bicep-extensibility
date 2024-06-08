// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Models;
using Json.Pointer;
using System.Linq.Expressions;

namespace Azure.Deployments.Extensibility.Core.V2.Validation
{
    public class PropertyRule<TModel, TProperty> : IModelValidationRule<TModel>
    {
        private readonly List<IPropertyRuleCriterion<TModel, TProperty>> criteria = [];
        private readonly Expression<Func<TModel, TProperty>> propertyExpression;

        public PropertyRule(Expression<Func<TModel, TProperty>> propertyExpression)
        {
            this.propertyExpression = propertyExpression;
        }

        public void AddCriterion(IPropertyRuleCriterion<TModel, TProperty> criterion) => this.criteria.Add(criterion);

        public IEnumerable<ErrorDetail> Validate(TModel model)
        {
            if (this.criteria.Count == 0)
            {
                yield break;
            }

            var property = this.propertyExpression.Compile().Invoke(model);
            var propertyPointer = this.CreatePropertyPointer();

            foreach (var criterion in this.criteria)
            {
                var criterionSatisfied = true;

                foreach (var errorDetail in criterion.Evaluate(model, property, propertyPointer))
                {
                    criterionSatisfied = false;
                    yield return errorDetail;
                }

                if (!criterionSatisfied)
                {
                    // Do not evaluate the next statement if the current one is not satisfied.
                    yield break;
                }
            }
        }

        private JsonPointer CreatePropertyPointer() => JsonPointer.Create(
            Expression.Lambda<Func<TModel, object>>(
                Expression.Convert(this.propertyExpression.Body, typeof(object)), this.propertyExpression.Parameters),
            new PointerCreationOptions
            {
                PropertyNameResolver = PropertyNameResolvers.CamelCase,
            });
    }
}
