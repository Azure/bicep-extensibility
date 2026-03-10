// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Exceptions;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.AspNetCore.Pipeline;

/// <summary>
/// A global pipeline behavior that catches <see cref="ErrorResponseException"/>
/// (including <see cref="Exceptions.HttpErrorResponseException"/>) and converts it
/// to an <see cref="ErrorResponse"/> result instead of letting it propagate as an unhandled exception.
/// </summary>
/// <remarks>
/// Registered by default in <see cref="ExtensionApplication"/>. Runs as the outermost
/// behavior so that all exceptions are captured regardless of other pipeline behaviors.
/// </remarks>
internal sealed class ErrorResponseExceptionHandlingBehavior :
    IResourcePreviewPipelineBehavior,
    IResourceCreateOrUpdatePipelineBehavior,
    IResourceGetPipelineBehavior,
    IResourceDeletePipelineBehavior,
    ILongRunningOperationGetPipelineBehavior
{
    Task<OneOf<ResourcePreview, ErrorResponse>> IResourcePreviewPipelineBehavior.HandleAsync(
        ResourcePreviewSpecification request, ResourcePreviewHandlerDelegate next, CancellationToken cancellationToken) =>
        ExecuteAsync(request, next.Invoke, static error => error);

    Task<OneOf<Resource, LongRunningOperation, ErrorResponse>> IResourceCreateOrUpdatePipelineBehavior.HandleAsync(
        ResourceSpecification request, ResourceCreateOrUpdateHandlerDelegate next, CancellationToken cancellationToken) =>
        ExecuteAsync(request, next.Invoke, static error => error);

    Task<OneOf<Resource?, ErrorResponse>> IResourceGetPipelineBehavior.HandleAsync(
        ResourceReference request, ResourceGetHandlerDelegate next, CancellationToken cancellationToken) =>
        ExecuteAsync(request, next.Invoke, static error => error);

    Task<OneOf<Resource?, LongRunningOperation, ErrorResponse>> IResourceDeletePipelineBehavior.HandleAsync(
        ResourceReference request, ResourceDeleteHandlerDelegate next, CancellationToken cancellationToken) =>
        ExecuteAsync(request, next.Invoke, static error => error);

    Task<OneOf<LongRunningOperation, ErrorResponse>> ILongRunningOperationGetPipelineBehavior.HandleAsync(
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
