// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Decorators;
using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;

namespace MagicEightBallExtension.Decorators;

/// <summary>
/// Base class for version-scoped API version validation decorators.
/// Subclasses supply the accepted API versions (e.g. stable + preview) via the constructor.
/// </summary>
public abstract class ApiVersionValidationBehaviorBase :
    IResourcePreviewDecorator,
    IResourceCreateOrUpdateDecorator,
    IResourceGetDecorator,
    IResourceDeleteDecorator
{
    private readonly IReadOnlySet<string> acceptedApiVersions;

    protected ApiVersionValidationBehaviorBase(params string[] acceptedApiVersions)
    {
        this.acceptedApiVersions = new HashSet<string>(acceptedApiVersions, StringComparer.OrdinalIgnoreCase);
    }

    Task<OneOf<Resource, LongRunningOperation, ErrorResponse>> IResourceCreateOrUpdateDecorator.HandleAsync(
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

    Task<OneOf<ResourcePreview, ErrorResponse>> IResourcePreviewDecorator.HandleAsync(
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

    Task<OneOf<Resource?, ErrorResponse>> IResourceGetDecorator.HandleAsync(
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

    Task<OneOf<Resource?, LongRunningOperation, ErrorResponse>> IResourceDeleteDecorator.HandleAsync(
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
