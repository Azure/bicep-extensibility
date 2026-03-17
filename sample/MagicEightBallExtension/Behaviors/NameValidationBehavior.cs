// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.AspNetCore.Behaviors;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;

namespace MagicEightBallExtension.Behaviors;

/// <summary>
/// A global decorator that validates resource operation inputs.
/// For create/update and preview, it validates that the required 'name' property is present.
/// For get and delete, it validates that the required 'name' identifier is present.
/// </summary>
public sealed class NameValidationBehavior : IResourceCreateOrUpdateBehavior, IResourcePreviewBehavior, IResourceGetBehavior, IResourceDeleteBehavior
{
    Task<OneOf<Resource, LongRunningOperation, ErrorResponse>> IResourceCreateOrUpdateBehavior.HandleAsync(
        ResourceSpecification request,
        ResourceCreateOrUpdateHandlerDelegate next,
        CancellationToken cancellationToken)
    {
        if (ValidateNameProperty(request.Properties["name"]?.GetValue<string>()) is { } error)
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
        if (ValidateNameProperty(request.Properties["name"]?.GetValue<string>()) is { } error)
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
        if (ValidateNameIdentifier(request.Identifiers["name"]?.GetValue<string>()) is { } error)
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
        if (ValidateNameIdentifier(request.Identifiers["name"]?.GetValue<string>()) is { } error)
        {
            return Task.FromResult<OneOf<Resource?, LongRunningOperation, ErrorResponse>>(error);
        }

        return next(request);
    }

    private static ErrorResponse? ValidateNameProperty(string? name) =>
        string.IsNullOrWhiteSpace(name)
            ? new ErrorResponse(new Error
            {
                Code = "MissingRequiredProperty",
                Message = "The 'name' property is required.",
                Target = Json.Pointer.JsonPointer.Parse("/properties/name"),
            })
            : null;

    private static ErrorResponse? ValidateNameIdentifier(string? name) =>
        string.IsNullOrWhiteSpace(name)
            ? new ErrorResponse(new Error
            {
                Code = "MissingRequiredIdentifier",
                Message = "The 'name' identifier is required.",
                Target = Json.Pointer.JsonPointer.Parse("/identifiers/name"),
            })
            : null;
}
