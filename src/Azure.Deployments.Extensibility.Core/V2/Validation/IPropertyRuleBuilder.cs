// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Deployments.Extensibility.Core.V2.Validation
{
    /// <summary>
    /// Non-generic marker interface for a property rule builder.
    /// Used as a dependency reference in <see cref="IPropertyRuleBuilder{TModel,TProperty}.DependsOn"/>.
    /// </summary>
    public interface IPropertyRuleBuilder { }

    /// <summary>
    /// Fluent builder for adding validation criteria to a property rule.
    /// </summary>
    /// <typeparam name="TModel">The type of model being validated.</typeparam>
    /// <typeparam name="TProperty">The type of the property being validated.</typeparam>
    public interface IPropertyRuleBuilder<TModel, out TProperty> : IPropertyRuleBuilder
    {
        public IPropertyRuleBuilder<TModel, TProperty> AddCriterion<TCriterion>(TCriterion criterion)
            where TCriterion : IPropertyRuleCriterion<TModel, TProperty>;

        /// <summary>
        /// Declares that this rule should be skipped if any of the specified dependency rules produced errors.
        /// </summary>
        public IPropertyRuleBuilder<TModel, TProperty> DependsOn(params IPropertyRuleBuilder[] dependencies);
    }
}
