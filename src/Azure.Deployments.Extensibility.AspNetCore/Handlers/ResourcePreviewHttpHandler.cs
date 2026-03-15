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
/// Abstract base class for HTTP-based resource preview handlers.
/// Provides model validation and <see cref="ErrorResponseException"/> handling.
/// </summary>
public abstract class ResourcePreviewHttpHandler : HttpContextAwareHandler, IResourcePreviewHandler
{
    protected ResourcePreviewHttpHandler(IHttpContextAccessor httpContextAccessor)
        : base(httpContextAccessor)
    {
    }

    public virtual async Task<OneOf<ResourcePreview, ErrorResponse>> HandleAsync(ResourcePreviewSpecification resourcePreviewSpecification, CancellationToken cancellationToken)
    {
        var modelValidator = this.HttpContext.RequestServices.GetService<IModelValidator<ResourcePreviewSpecification>>();

        if (modelValidator?.Validate(resourcePreviewSpecification) is { } error)
        {
            return new ErrorResponse(error);
        }

        try
        {
            return await this.PreviewResourceAsync(resourcePreviewSpecification, cancellationToken);
        }
        catch (ErrorResponseException errorResponseException)
        {
            return errorResponseException.ToErrorResponse();
        }
    }

    protected abstract Task<OneOf<ResourcePreview, ErrorResponse>> PreviewResourceAsync(ResourcePreviewSpecification resourcePreviewSpecification, CancellationToken cancellationToken);
}
