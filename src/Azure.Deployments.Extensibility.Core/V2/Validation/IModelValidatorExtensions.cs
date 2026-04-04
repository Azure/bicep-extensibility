// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts.Exceptions;
using Azure.Deployments.Extensibility.Core.V2.Validation;

namespace Azure.Deployments.Extensibility.Core.V2.Validation
{
    /// <summary>
    /// Extension methods for <see cref="IModelValidator{TModel}"/>.
    /// </summary>
    public static class IModelValidatorExtensions
    {
        /// <summary>
        /// Validate the model and throw an <see cref="ErrorResponseException"/> if validation fails.
        /// </summary>
        /// <typeparam name="TModel">The type of model to validate.</typeparam>
        /// <param name="validator">The validator instance.</param>
        /// <param name="model">The model instance to validate.</param>
        /// <exception cref="ErrorResponseException">Thrown when validation fails.</exception>
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
