// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Deployments.Extensibility.Core.V2.Validation
{
    internal interface IPropertyRuleBuilderInternal : IPropertyRuleBuilder
    {
        IReadOnlyList<IPropertyRuleBuilder> Dependencies { get; }
    }

    internal class PropertyRuleBuilder<TModel, TProperty> : IPropertyRuleBuilder<TModel, TProperty>, IPropertyRuleBuilderInternal
    {
        private readonly PropertyRule<TModel, TProperty> rule;
        private readonly List<IPropertyRuleBuilder> dependencies = [];

        public PropertyRuleBuilder(PropertyRule<TModel, TProperty> rule)
        {
            this.rule = rule;
        }

        public IReadOnlyList<IPropertyRuleBuilder> Dependencies => this.dependencies;

        public IPropertyRuleBuilder<TModel, TProperty> AddCriterion<TCriterion>(TCriterion criterion)
            where TCriterion : IPropertyRuleCriterion<TModel, TProperty>
        {
            this.rule.AddCriterion(criterion);

            return this;
        }

        public IPropertyRuleBuilder<TModel, TProperty> DependsOn(params IPropertyRuleBuilder[] dependencies)
        {
            this.dependencies.AddRange(dependencies);

            return this;
        }
    }
}
