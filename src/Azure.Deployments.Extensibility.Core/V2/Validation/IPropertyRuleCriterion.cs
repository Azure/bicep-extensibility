// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using Json.Pointer;

namespace Azure.Deployments.Extensibility.Core.V2.Validation
{
    /// <summary>
    /// Defines a validation criterion that evaluates a property value and yields error details on failure.
    /// </summary>
    /// <typeparam name="TModel">The type of model being validated.</typeparam>
    /// <typeparam name="TProperty">The type of the property being validated.</typeparam>
    public interface IPropertyRuleCriterion<TModel, in TProperty>
    {
        public string ErrorCode { get; set; }

        public string ErrorMessage { get; set; }

        public IEnumerable<ErrorDetail> Evaluate(TModel model, TProperty propertyValue, JsonPointer propertyPointer);
    }
}
