// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using Json.Pointer;

namespace Azure.Deployments.Extensibility.Core.V2.Validation.Criteria
{
    /// <summary>
    /// A validation criterion that fails when the property value does not satisfy the specified predicate.
    /// </summary>
    public class SatisfiesCriterion<TModel, TProperty>(Func<TProperty, bool> predicate) : IPropertyRuleCriterion<TModel, TProperty>, IConfigurableErrorCriterion
    {
        /// <inheritdoc/>
        public string ErrorCode { get; set; } = "ConditionNotSatisfied";

        /// <inheritdoc/>
        public string ErrorMessage { get; set; } = "Value does not satisfy the required condition.";

        /// <inheritdoc/>
        public IEnumerable<ErrorDetail> Evaluate(TModel model, TProperty propertyValue, JsonPointer propertyPointer)
        {
            if (!predicate(propertyValue))
            {
                yield return new(this.ErrorCode, this.ErrorMessage, propertyPointer);
            }
        }
    }
}
