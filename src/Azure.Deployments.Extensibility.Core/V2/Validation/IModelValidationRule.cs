// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Models;

namespace Azure.Deployments.Extensibility.Core.V2.Validation
{
    public interface IModelValidationRule<T>
    {
        public bool BailOnError { get; set; }

        public IEnumerable<ErrorDetail> Validate(T model);
    }
}
