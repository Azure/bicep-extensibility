// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Exceptions;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.AspNetCore.Decorators;

/// <summary>
/// A global decorator that catches <see cref="ErrorResponseException"/>
/// (including <see cref="Exceptions.HttpErrorResponseException"/>) and converts it
/// to an <see cref="ErrorResponse"/> result instead of letting it propagate as an unhandled exception.
/// </summary>
/// <remarks>
/// Registered by default in <see cref="ExtensionApplication"/>. Runs as the outermost
/// decorator so that all exceptions are captured regardless of other decorators.
/// </remarks>
internal sealed class ErrorResponseExceptionHandlingBehavior :
    IResourcePreviewDecorator,
    IResourceCreateOrUpdateDecorator,
    IResourceGetDecorator,
    IResourceDeleteDecorator,
    ILongRunningOperationGetDecorator
{
    Task<OneOf<ResourcePreview, ErrorResponse>> IResourcePreviewDecorator.HandleAsync(
        ResourcePreviewSpecification request, ResourcePreviewHandlerDelegate next, CancellationToken cancellationToken) =>
        ExecuteAsync(request, next.Invoke, static error => error);

    Task<OneOf<Resource, LongRunningOperation, ErrorResponse>> IResourceCreateOrUpdateDecorator.HandleAsync(
        ResourceSpecification request, ResourceCreateOrUpdateHandlerDelegate next, CancellationToken cancellationToken) =>
        ExecuteAsync(request, next.Invoke, static error => error);

    Task<OneOf<Resource?, ErrorResponse>> IResourceGetDecorator.HandleAsync(
        ResourceReference request, ResourceGetHandlerDelegate next, CancellationToken cancellationToken) =>
        ExecuteAsync(request, next.Invoke, static error => error);

    Task<OneOf<Resource?, LongRunningOperation, ErrorResponse>> IResourceDeleteDecorator.HandleAsync(
        ResourceReference request, ResourceDeleteHandlerDelegate next, CancellationToken cancellationToken) =>
        ExecuteAsync(request, next.Invoke, static error => error);

    Task<OneOf<LongRunningOperation, ErrorResponse>> ILongRunningOperationGetDecorator.HandleAsync(
        JsonObject request, LongRunningOperationGetHandlerDelegate next, CancellationToken cancellationToken) =>
        ExecuteAsync(request, next.Invoke, static error => error);

    private static async Task<TResponse> ExecuteAsync<TRequest, TResponse>(
        TRequest request,
        Func<TRequest, Task<TResponse>> next,
        Func<ErrorResponse, TResponse> toResponse)
    {
        try
        {
            return await next(request);
        }
        catch (ErrorResponseException ex)
        {
            return toResponse(ex.ToErrorResponse());
        }
    }
}
