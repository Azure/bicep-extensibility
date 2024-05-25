// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Deployments.Extensibility.Core.V2.Validation
{
    public interface IPropertyRuleBuilder<TModel, out TProperty>
    {
        public IPropertyRuleBuilder<TModel, TProperty> AddCriterion<TCriterion>(TCriterion criterion)
            where TCriterion : IPropertyRuleCriterion<TModel, TProperty>;
    }
}
