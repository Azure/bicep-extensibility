// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;

namespace Azure.Deployments.Extensibility.Core.V2.Validation;

/// <summary>
/// A no-op validator that always returns valid (null) for any model.
/// </summary>
/// <typeparam name="TModel">The type of model to validate.</typeparam>
public class AlwaysValidValidator<TModel> : IModelValidator<TModel>
    where TModel : class
{
    /// <inheritdoc/>
    public Error? Validate(TModel model) => null;
}
