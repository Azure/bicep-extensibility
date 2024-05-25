// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Models;
using Json.Pointer;

namespace Azure.Deployments.Extensibility.Core.V2.Validation.Criteria
{
    public class NotBeNullCriterion<TModel, TProperty> : IPropertyRuleCriterion<TModel, TProperty>
    {
        public string ErrorCode { get; set; } = "ValueMustNotBeNull";

        public string ErrorMessage { get; set; } = "Value must not be null.";

        public IEnumerable<ErrorDetail> Evaluate(TModel model, TProperty propertyValue, JsonPointer propertyPointer)
        {
            if (propertyValue is null)
            {
                yield return new(this.ErrorCode, this.ErrorMessage, propertyPointer);
            }
        }
    }
}
