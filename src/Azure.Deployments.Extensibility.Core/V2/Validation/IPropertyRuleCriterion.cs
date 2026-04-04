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
        /// <summary>
        /// Evaluate the criterion against the specified property value.
        /// </summary>
        /// <param name="model">The model being validated.</param>
        /// <param name="propertyValue">The property value to evaluate.</param>
        /// <param name="propertyPointer">The JSON Pointer to the property.</param>
        /// <returns>An enumerable of <see cref="ErrorDetail"/> instances for each violation found.</returns>
        public IEnumerable<ErrorDetail> Evaluate(TModel model, TProperty propertyValue, JsonPointer propertyPointer);
    }
}
