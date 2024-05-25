// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Models;
using Json.Pointer;

namespace Azure.Deployments.Extensibility.Core.V2.Validation
{
    public interface IPropertyRuleCriterion<TModel, in TProperty>
    {
        public string ErrorCode { get; set; }

        public string ErrorMessage { get; set; }

        public IEnumerable<ErrorDetail> Evaluate(TModel model, TProperty propertyValue, JsonPointer propertyPointer);
    }
}
