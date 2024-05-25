// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Deployments.Extensibility.Core.V2.Validation
{
    public class PropertyRuleBuilder<TModel, TProperty> : IPropertyRuleBuilder<TModel, TProperty>
    {
        private readonly PropertyRule<TModel, TProperty> rule;

        public PropertyRuleBuilder(PropertyRule<TModel, TProperty> rule)
        {
            this.rule = rule;
        }

        public IPropertyRuleBuilder<TModel, TProperty> AddCriterion<TCriterion>(TCriterion criterion)
            where TCriterion : IPropertyRuleCriterion<TModel, TProperty>
        {
            this.rule.AddCriterion(criterion);

            return this;
        }
    }
}
