// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts.Exceptions;
using Azure.Deployments.Extensibility.Core.V2.Validation;

namespace Azure.Deployments.Extensibility.AspNetCore.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="IModelValidator{TModel}"/>.
    /// </summary>
    public static class IModelValidatorExtensions
    {
        public static void ValidateAndThrow<TModel>(this IModelValidator<TModel> validator, TModel model)
            where TModel : class
        {
            if (validator.Validate(model) is { } error)
            {
                throw new ErrorResponseException(error);
            }
        }
    }
}
