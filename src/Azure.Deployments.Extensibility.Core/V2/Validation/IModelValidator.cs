// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Models;

namespace Azure.Deployments.Extensibility.Core.V2.Validation
{
    public interface IModelValidator<TModel>
        where TModel : class
    {
        Error? Validate(TModel model);
    }
}
