// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Exceptions;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using Azure.Deployments.Extensibility.Core.V2.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Azure.Deployments.Extensibility.AspNetCore.Handlers;

/// <summary>
/// Abstract base class for HTTP-based resource create-or-update handlers.
/// Provides model validation and <see cref="ErrorResponseException"/> handling.
/// </summary>
public abstract class ResourceCreateOrUpdateHttpHandler : HttpContextAwareHandler, IResourceCreateOrUpdateHandler
{
    protected ResourceCreateOrUpdateHttpHandler(IHttpContextAccessor httpContextAccessor)
        : base(httpContextAccessor)
    {
    }

    public virtual async Task<OneOf<Resource, LongRunningOperation, ErrorResponse>> HandleAsync(ResourceSpecification resourceSpecification, CancellationToken cancellationToken)
    {
        var modelValidator = this.HttpContext.RequestServices.GetService<IModelValidator<ResourceSpecification>>();

        if (modelValidator?.Validate(resourceSpecification) is { } error)
        {
            return new ErrorResponse(error);
        }

        try
        {
            return await this.CreateOrUpdateResourceAsync(resourceSpecification, cancellationToken);
        }
        catch (ErrorResponseException errorResponseException)
        {
            return errorResponseException.ToErrorResponse();
        }
    }

    protected abstract Task<OneOf<Resource, LongRunningOperation, ErrorResponse>> CreateOrUpdateResourceAsync(ResourceSpecification resourceSpecification, CancellationToken cancellationToken);
}
