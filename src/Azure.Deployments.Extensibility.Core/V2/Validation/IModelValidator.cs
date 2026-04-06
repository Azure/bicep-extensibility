// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;

namespace Azure.Deployments.Extensibility.Core.V2.Validation
{
    /// <summary>
    /// Defines a validator that can validate a model and return an error if validation fails.
    /// </summary>
    /// <typeparam name="TModel">The type of model to validate.</typeparam>
    public interface IModelValidator<TModel>
        where TModel : class
    {
        /// <summary>
        /// Validate the specified model.
        /// </summary>
        /// <param name="model">The model instance to validate.</param>
        /// <returns>An <see cref="Error"/> if validation fails; otherwise, <see langword="null"/>.</returns>
        Error? Validate(TModel model);
    }
}
