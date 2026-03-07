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
/// Abstract base class for HTTP-based resource get handlers.
/// Provides model validation and <see cref="ErrorResponseException"/> handling.
/// </summary>
public abstract class ResourceGetHttpHandler : HttpContextAwareHandler, IResourceGetHandler
{
    protected ResourceGetHttpHandler(IHttpContextAccessor httpContextAccessor)
        : base(httpContextAccessor)
    {
    }

    public virtual async Task<OneOf<Resource?, ErrorResponse>> HandleAsync(ResourceReference resourceReference, CancellationToken cancellationToken)
    {
        var modelValidator = this.HttpContext.RequestServices.GetService<IModelValidator<ResourceReference>>();

        if (modelValidator?.Validate(resourceReference) is { } error)
        {
            return new ErrorResponse(error);
        }

        try
        {
            return await this.GetResourceAsync(resourceReference, cancellationToken);
        }
        catch (ErrorResponseException errorResponseException)
        {
            return errorResponseException.ToErrorResponse();
        }
    }

    protected abstract Task<OneOf<Resource?, ErrorResponse>> GetResourceAsync(ResourceReference resourceReference, CancellationToken cancellationToken);
}
