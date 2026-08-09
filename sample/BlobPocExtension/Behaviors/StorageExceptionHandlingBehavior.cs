// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure;
using Azure.Deployments.Extensibility.AspNetCore.Behaviors;
using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using BlobPocExtension.Storage;
using System.Text.Json.Nodes;

namespace BlobPocExtension.Behaviors;

/// <summary>
/// Wraps every resource operation and maps any <see cref="RequestFailedException"/> thrown by a handler
/// to a stable <see cref="ErrorResponse"/> via <see cref="StorageErrorMapper"/>. This lets the handlers
/// perform storage I/O without repeating a <c>try/catch</c> in each one.
/// </summary>
public sealed class StorageExceptionHandlingBehavior :
    IResourcePreviewBehavior,
    IResourceCreateOrUpdateBehavior,
    IResourceGetBehavior,
    IResourceDeleteBehavior
{
    Task<OneOf<ResourcePreview, ErrorResponse>> IResourcePreviewBehavior.HandleAsync(
        ResourcePreviewSpecification request, ResourcePreviewHandlerDelegate next, CancellationToken cancellationToken) =>
        ExecuteAsync(request, next.Invoke, static error => error);

    Task<OneOf<Resource, LongRunningOperation, ErrorResponse>> IResourceCreateOrUpdateBehavior.HandleAsync(
        ResourceSpecification request, ResourceCreateOrUpdateHandlerDelegate next, CancellationToken cancellationToken) =>
        ExecuteAsync(request, next.Invoke, static error => error);

    Task<OneOf<Resource?, ErrorResponse>> IResourceGetBehavior.HandleAsync(
        ResourceReference request, ResourceGetHandlerDelegate next, CancellationToken cancellationToken) =>
        ExecuteAsync(request, next.Invoke, static error => error);

    Task<OneOf<Resource?, LongRunningOperation, ErrorResponse>> IResourceDeleteBehavior.HandleAsync(
        ResourceReference request, ResourceDeleteHandlerDelegate next, CancellationToken cancellationToken) =>
        ExecuteAsync(request, next.Invoke, static error => error);

    private static async Task<TResponse> ExecuteAsync<TRequest, TResponse>(
        TRequest request, Func<TRequest, Task<TResponse>> next, Func<ErrorResponse, TResponse> toResponse)
    {
        try
        {
            return await next(request);
        }
        catch (RequestFailedException exception)
        {
            return toResponse(StorageErrorMapper.MapStorageError(exception));
        }
    }
}
