// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Models;
using Azure.Deployments.Extensibility.AspNetCore.Pipeline;
using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using Microsoft.AspNetCore.Http;
using System.Text.Json.Nodes;

using static Microsoft.AspNetCore.Http.TypedResults;

namespace Azure.Deployments.Extensibility.AspNetCore.Handlers;

internal static class HandlerDispatcher
{
    public static async Task<IResult> DispatchResourcePreviewHandlerAsync(
        string extensionVersion,
        ResourcePreviewSpecification request,
        HandlerRegistry handlerRegistry,
        HandlerPipelineRegistry pipelineRegistry,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var (handler, versionRange) = handlerRegistry.ResolveHandler<IResourcePreviewHandler>(extensionVersion, request.Type, serviceProvider);
        var behaviors = pipelineRegistry.Resolve<ResourcePreviewSpecification, OneOf<ResourcePreview, ErrorResponse>>(handler.GetType(), versionRange, serviceProvider);

        var response = await ExecutePipelineAsync(request, behaviors, req => handler.HandleAsync(req, cancellationToken), cancellationToken);

        return response.Match(
            resourcePreview => Ok(resourcePreview),
            errorResponse => ErrorResponseToHttpResult(errorResponse));
    }

    public static async Task<IResult> DispatchResourceCreateOrUpdateHandlerAsync(
        string extensionVersion,
        ResourceSpecification specification,
        HandlerRegistry handlerRegistry,
        HandlerPipelineRegistry pipelineRegistry,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var (handler, versionRange) = handlerRegistry.ResolveHandler<IResourceCreateOrUpdateHandler>(extensionVersion, specification.Type, serviceProvider);
        var behaviors = pipelineRegistry.Resolve<ResourceSpecification, OneOf<Resource, LongRunningOperation, ErrorResponse>>(
            handler.GetType(), versionRange, serviceProvider);

        var response = await ExecutePipelineAsync(
            specification, behaviors,
            req => handler.HandleAsync(req, cancellationToken),
            cancellationToken);

        return response.Match(
            resource => Ok(resource),
            longRunningOperation => Accepted(uri: (string?)null, longRunningOperation),
            errorResponse => ErrorResponseToHttpResult(errorResponse));
    }

    public static async Task<IResult> DispatchResourceGetHandlerAsync(
        string extensionVersion,
        ResourceReference reference,
        HandlerRegistry handlerRegistry,
        HandlerPipelineRegistry pipelineRegistry,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var (handler, versionRange) = handlerRegistry.ResolveHandler<IResourceGetHandler>(extensionVersion, reference.Type, serviceProvider);
        var behaviors = pipelineRegistry.Resolve<ResourceReference, OneOf<Resource?, ErrorResponse>>(
            handler.GetType(), versionRange, serviceProvider);

        var response = await ExecutePipelineAsync(
            reference, behaviors,
            req => handler.HandleAsync(req, cancellationToken),
            cancellationToken);

        return response.Match(
            resource => resource is not null ? Ok(resource) : NotFound(),
            errorResponse => ErrorResponseToHttpResult(errorResponse));
    }

    public static async Task<IResult> DispatchResourceDeleteHandlerAsync(
        string extensionVersion,
        ResourceReference reference,
        HandlerRegistry handlerRegistry,
        HandlerPipelineRegistry pipelineRegistry,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var (handler, versionRange) = handlerRegistry.ResolveHandler<IResourceDeleteHandler>(extensionVersion, reference.Type, serviceProvider);
        var behaviors = pipelineRegistry.Resolve<ResourceReference, OneOf<Resource?, LongRunningOperation, ErrorResponse>>(
            handler.GetType(), versionRange, serviceProvider);

        var response = await ExecutePipelineAsync(
            reference, behaviors,
            req => handler.HandleAsync(req, cancellationToken),
            cancellationToken);

        return response.Match(
            resource => resource is not null ? Ok(resource) : NoContent(),
            longRunningOperation => Accepted(uri: (string?)null, longRunningOperation),
            errorResponse => ErrorResponseToHttpResult(errorResponse));
    }


    // An error result here indicates an issue retrieving the operation status (e.g., network errors),
    // NOT a failure of the operation itself. Operation failures are reported via the operation's status field.
    public static async Task<IResult> DispatchLongRunningOperationGetHandlerAsync(
        string extensionVersion,
        JsonObject operationHandle,
        HandlerRegistry handlerRegistry,
        HandlerPipelineRegistry pipelineRegistry,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var (handler, versionRange) = handlerRegistry.ResolveHandler<ILongRunningOperationGetHandler>(extensionVersion, resourceType: null, serviceProvider);
        var behaviors = pipelineRegistry.Resolve<JsonObject, OneOf<LongRunningOperation, ErrorResponse>>(
            handler.GetType(), versionRange, serviceProvider);

        var response = await ExecutePipelineAsync(
            operationHandle, behaviors,
            req => handler.HandleAsync(req, cancellationToken),
            cancellationToken);

        return response.Match(
            longRunningOperation => Ok(longRunningOperation),
            errorResponse => ErrorResponseToHttpResult(errorResponse));
    }

    private static Task<TResponse> ExecutePipelineAsync<TRequest, TResponse>(
        TRequest request,
        IReadOnlyList<IHandlerPipelineBehavior<TRequest, TResponse>> behaviors,
        HandlerDelegate<TRequest, TResponse> innerHandler,
        CancellationToken cancellationToken)
    {
        HandlerDelegate<TRequest, TResponse> next = innerHandler;

        // Build chain from inside out - first registered behavior is outermost.
        for (var i = behaviors.Count - 1; i >= 0; i--)
        {
            var behavior = behaviors[i];
            var current = next;
            next = req => behavior.HandleAsync(req, current, cancellationToken);
        }

        return next(request);
    }

    private static IResult ErrorResponseToHttpResult(ErrorResponse errorResponse) => errorResponse is HttpErrorResponse httpErrorResponse
        ? TypedResults.Json(httpErrorResponse.AsErrorResponse(), statusCode: httpErrorResponse.StatusCode)
        : BadRequest(errorResponse);
}
