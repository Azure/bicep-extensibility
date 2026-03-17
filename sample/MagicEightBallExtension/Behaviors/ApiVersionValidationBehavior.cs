// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Behaviors;
using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;

namespace MagicEightBallExtension.Behaviors;

/// <summary>
/// Validates that the resource API version is one of the accepted versions supplied at construction time.
/// Use the factory overload of <c>AddHandlerBehavior</c> to pass the accepted versions:
/// <code>
/// .AddHandlerBehavior(sp => new ApiVersionValidationBehavior("2024-01-01", "2024-01-01-preview"))
/// </code>
/// </summary>
public sealed class ApiVersionValidationBehavior :
    IResourcePreviewBehavior,
    IResourceCreateOrUpdateBehavior,
    IResourceGetBehavior,
    IResourceDeleteBehavior
{
    private readonly IReadOnlySet<string> acceptedApiVersions;

    public ApiVersionValidationBehavior(params string[] acceptedApiVersions)
    {
        this.acceptedApiVersions = new HashSet<string>(acceptedApiVersions, StringComparer.OrdinalIgnoreCase);
    }

    Task<OneOf<Resource, LongRunningOperation, ErrorResponse>> IResourceCreateOrUpdateBehavior.HandleAsync(
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

    Task<OneOf<ResourcePreview, ErrorResponse>> IResourcePreviewBehavior.HandleAsync(
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

    Task<OneOf<Resource?, ErrorResponse>> IResourceGetBehavior.HandleAsync(
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

    Task<OneOf<Resource?, LongRunningOperation, ErrorResponse>> IResourceDeleteBehavior.HandleAsync(
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
