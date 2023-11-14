// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Deployments.Extensibility.Core.V2.Models.Validation
{
    public interface IResourceTypeValidator : IValidator<string, IReadOnlyList<ErrorDetail>>
    {
    }
}
