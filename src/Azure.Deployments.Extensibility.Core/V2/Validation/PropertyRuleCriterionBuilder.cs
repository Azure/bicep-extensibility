// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Deployments.Extensibility.Core.V2.Validation
{
    public class PropertyRuleCriterionBuilder<TModel, TProperty>
    {
        private readonly IPropertyRuleBuilder<TModel, TProperty> ruleBuilder;
        private readonly IPropertyRuleCriterion<TModel, TProperty> criterion;

        public PropertyRuleCriterionBuilder(IPropertyRuleBuilder<TModel, TProperty> ruleBuilder, IPropertyRuleCriterion<TModel, TProperty> criterion)
        {
            this.ruleBuilder = ruleBuilder;
            this.criterion = criterion;
        }

        public IPropertyRuleBuilder<TModel, TProperty> AndThen => this.ruleBuilder;

        public PropertyRuleCriterionBuilder<TModel, TProperty> WithErrorCode(string errorCode)
        {
            this.criterion.ErrorCode = errorCode;

            return this;
        }

        public PropertyRuleCriterionBuilder<TModel, TProperty> WithErrorMessage(string errorMessage)
        {
            this.criterion.ErrorMessage = errorMessage;

            return this;
        }
    }
}
