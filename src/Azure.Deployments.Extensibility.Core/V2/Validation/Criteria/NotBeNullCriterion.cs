// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using Json.Pointer;

namespace Azure.Deployments.Extensibility.Core.V2.Validation.Criteria
{
    /// <summary>
    /// A validation criterion that fails when the property value is null.
    /// </summary>
    public class NotBeNullCriterion<TModel, TProperty> : IPropertyRuleCriterion<TModel, TProperty>, IConfigurableErrorCriterion
    {
        /// <inheritdoc/>
        public string ErrorCode { get; set; } = "ValueMustNotBeNull";

        /// <inheritdoc/>
        public string ErrorMessage { get; set; } = "Value must not be null.";

        /// <inheritdoc/>
        public IEnumerable<ErrorDetail> Evaluate(TModel model, TProperty propertyValue, JsonPointer propertyPointer)
        {
            if (propertyValue is null)
            {
                yield return new(this.ErrorCode, this.ErrorMessage, propertyPointer);
            }
        }
    }
}
