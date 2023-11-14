// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Deployments.Extensibility.Core.V2.Models.Validation
{
    public interface IValidator<TValue, out TResult>
    {
        TResult Validate(TValue value);
    }
}
