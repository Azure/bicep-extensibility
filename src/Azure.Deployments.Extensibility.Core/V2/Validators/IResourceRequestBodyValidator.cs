// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Models;

namespace Azure.Deployments.Extensibility.Core.V2.Validators
{
    public interface IResourceRequestBodyValidator : IValidator<ResourceRequestBody, Error?>
    {
    }
}
