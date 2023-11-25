// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Models;

namespace Azure.Deployments.Extensibility.Core.V2.Validators
{
    public interface IResourceTypeValidator : IValidator<string, IReadOnlyList<ErrorDetail>>
    {
    }
}
