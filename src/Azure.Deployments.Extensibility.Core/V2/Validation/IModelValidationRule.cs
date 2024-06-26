// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Models;

namespace Azure.Deployments.Extensibility.Core.V2.Validation
{
    public interface IModelValidationRule<TModel>
    {
        public IEnumerable<ErrorDetail> Validate(TModel model);
    }
}
