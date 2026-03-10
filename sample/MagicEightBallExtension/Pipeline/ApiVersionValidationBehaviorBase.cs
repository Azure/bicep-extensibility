// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Pipeline;
using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;

namespace MagicEightBallExtension.Pipeline;

/// <summary>
/// Base class for version-scoped API version validation behaviors.
/// Subclasses supply the accepted API versions (e.g. stable + preview) via the constructor.
/// </summary>
public abstract class ApiVersionValidationBehaviorBase :
    IResourcePreviewPipelineBehavior,
    IResourceCreateOrUpdatePipelineBehavior,
    IResourceGetPipelineBehavior,
    IResourceDeletePipelineBehavior
{
    private readonly IReadOnlySet<string> acceptedApiVersions;

    protected ApiVersionValidationBehaviorBase(params string[] acceptedApiVersions)
    {
        this.acceptedApiVersions = new HashSet<string>(acceptedApiVersions, StringComparer.OrdinalIgnoreCase);
    }

    Task<OneOf<Resource, LongRunningOperation, ErrorResponse>> IResourceCreateOrUpdatePipelineBehavior.HandleAsync(
        ResourceSpecification request,
        ResourceCreateOrUpdateHandlerDelegate next,
        CancellationToken cancellationToken)
    {
        if (Validate(request.ApiVersion) is { } error)
        {
            return Task.FromResult<OneOf<Resource, LongRunningOperation, ErrorResponse>>(error);
        }

        return next(request);
    }

    Task<OneOf<ResourcePreview, ErrorResponse>> IResourcePreviewPipelineBehavior.HandleAsync(
        ResourcePreviewSpecification request,
        ResourcePreviewHandlerDelegate next,
        CancellationToken cancellationToken)
    {
        if (Validate(request.ApiVersion) is { } error)
        {
            return Task.FromResult<OneOf<ResourcePreview, ErrorResponse>>(error);
        }

        return next(request);
    }

    Task<OneOf<Resource?, ErrorResponse>> IResourceGetPipelineBehavior.HandleAsync(
        ResourceReference request,
        ResourceGetHandlerDelegate next,
        CancellationToken cancellationToken)
    {
        if (Validate(request.ApiVersion) is { } error)
        {
            return Task.FromResult<OneOf<Resource?, ErrorResponse>>(error);
        }

        return next(request);
    }

    Task<OneOf<Resource?, LongRunningOperation, ErrorResponse>> IResourceDeletePipelineBehavior.HandleAsync(
        ResourceReference request,
        ResourceDeleteHandlerDelegate next,
        CancellationToken cancellationToken)
    {
        if (Validate(request.ApiVersion) is { } error)
        {
            return Task.FromResult<OneOf<Resource?, LongRunningOperation, ErrorResponse>>(error);
        }

        return next(request);
    }

    private ErrorResponse? Validate(string? apiVersion) =>
        apiVersion is null || !this.acceptedApiVersions.Contains(apiVersion)
            ? new ErrorResponse(new Error
            {
                Code = "UnsupportedApiVersion",
                Message = $"The API version '{apiVersion}' is not supported. Accepted versions: {string.Join(", ", this.acceptedApiVersions.Order())}.",
                Target = Json.Pointer.JsonPointer.Parse("/apiVersion"),
            })
            : null;
}
