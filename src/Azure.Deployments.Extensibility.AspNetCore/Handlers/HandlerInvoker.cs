// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Behaviors;
using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Exceptions;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using Microsoft.Extensions.Options;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.AspNetCore.Handlers;

internal sealed class HandlerInvoker
{
    private readonly IBicepExtensionResolver resolver;
    private readonly IServiceProvider serviceProvider;
    private readonly BicepExtensionRuntimeOptions options;

    public HandlerInvoker(
        IBicepExtensionResolver resolver,
        IServiceProvider serviceProvider,
        IOptions<BicepExtensionRuntimeOptions> options)
    {
        this.resolver = resolver;
        this.serviceProvider = serviceProvider;
        this.options = options.Value;
    }

    internal Task<OneOf<ResourcePreview, ErrorResponse>> InvokeResourcePreviewAsync(
        string extensionVersion,
        ResourcePreviewSpecification request,
        CancellationToken cancellationToken) => this.InvokeAsync<
            IResourcePreviewHandler,
            ResourcePreviewSpecification,
            OneOf<ResourcePreview, ErrorResponse>>(
                extensionVersion,
                request.Type,
                request,
                new ErrorResponse(new Error(
                    "UnsupportedResourceType",
                    $"The resource type '{request.Type}' is not supported.")),
                static errorResponse => errorResponse,
                cancellationToken);

    internal Task<OneOf<Resource, LongRunningOperation, ErrorResponse>> InvokeResourceCreateOrUpdateAsync(
        string extensionVersion,
        ResourceSpecification request,
        CancellationToken cancellationToken) => this.InvokeAsync<
            IResourceCreateOrUpdateHandler,
            ResourceSpecification,
            OneOf<Resource, LongRunningOperation, ErrorResponse>>(
                extensionVersion,
                request.Type,
                request,
                new ErrorResponse(new Error(
                    "UnsupportedResourceType",
                    $"The resource type '{request.Type}' is not supported.")),
                static errorResponse => errorResponse,
                cancellationToken);

    internal Task<OneOf<Resource?, ErrorResponse>> InvokeResourceGetAsync(
        string extensionVersion,
        ResourceReference request,
        CancellationToken cancellationToken) => this.InvokeAsync<
            IResourceGetHandler,
            ResourceReference,
            OneOf<Resource?, ErrorResponse>>(
                extensionVersion,
                request.Type,
                request,
                new ErrorResponse(new Error(
                    "UnsupportedResourceType",
                    $"The resource type '{request.Type}' is not supported.")),
                static errorResponse => errorResponse,
                cancellationToken);

    internal Task<OneOf<Resource?, LongRunningOperation, ErrorResponse>> InvokeResourceDeleteAsync(
        string extensionVersion,
        ResourceReference request,
        CancellationToken cancellationToken) => this.InvokeAsync<
            IResourceDeleteHandler,
            ResourceReference,
            OneOf<Resource?, LongRunningOperation, ErrorResponse>>(
                extensionVersion,
                request.Type,
                request,
                new ErrorResponse(new Error(
                    "UnsupportedResourceType",
                    $"The resource type '{request.Type}' is not supported.")),
                static errorResponse => errorResponse,
                cancellationToken);

    internal Task<OneOf<LongRunningOperation, ErrorResponse>> InvokeLongRunningOperationGetAsync(
        string extensionVersion,
        JsonObject request,
        CancellationToken cancellationToken) => this.InvokeAsync<
            ILongRunningOperationGetHandler,
            JsonObject,
            OneOf<LongRunningOperation, ErrorResponse>>(
                extensionVersion,
                resourceType: null,
                request,
                new ErrorResponse(new Error(
                    "UnsupportedOperation",
                    "Long-running operation status retrieval is not supported.")),
                static errorResponse => errorResponse,
                cancellationToken);

    private async Task<TResponse> InvokeAsync<THandler, TRequest, TResponse>(
        string extensionVersion,
        string? resourceType,
        TRequest request,
        ErrorResponse missingHandlerResponse,
        Func<ErrorResponse, TResponse> toResponse,
        CancellationToken cancellationToken)
        where THandler : class, IHandler<TRequest, TResponse>
    {
        var registration = new Lazy<BicepExtensionRegistration?>(
            () => this.resolver.Resolve(extensionVersion),
            LazyThreadSafetyMode.ExecutionAndPublication);

        HandlerDelegate<TRequest, TResponse> invokeRegistration = async currentRequest =>
        {
            if (registration.Value is not { } resolvedRegistration)
            {
                return toResponse(new ErrorResponse(new Error(
                    "UnsupportedExtensionVersion",
                    $"No handler found for extension version '{extensionVersion}'.")));
            }

            var handler = resolvedRegistration.ResolveHandler<THandler>(resourceType, this.serviceProvider);

            if (handler is null)
            {
                return toResponse(missingHandlerResponse);
            }

            var behaviors = resolvedRegistration.ResolveBehaviors<TRequest, TResponse>(resourceType, this.serviceProvider);

            return await ExecuteBehaviorChainAsync(
                currentRequest,
                behaviors,
                innerRequest => handler.HandleAsync(innerRequest, cancellationToken),
                cancellationToken);
        };

        try
        {
            var globalBehaviors = this.options.GlobalBehaviors
                .Select(behavior => behavior.Resolve(this.serviceProvider))
                .OfType<IHandlerBehavior<TRequest, TResponse>>()
                .ToArray();

            return await ExecuteBehaviorChainAsync(
                request,
                globalBehaviors,
                invokeRegistration,
                cancellationToken);
        }
        catch (ErrorResponseException exception)
        {
            return toResponse(exception.ToErrorResponse());
        }
    }

    private static Task<TResponse> ExecuteBehaviorChainAsync<TRequest, TResponse>(
        TRequest request,
        IReadOnlyList<IHandlerBehavior<TRequest, TResponse>> behaviors,
        HandlerDelegate<TRequest, TResponse> innerHandler,
        CancellationToken cancellationToken)
    {
        var next = innerHandler;

        for (var index = behaviors.Count - 1; index >= 0; index--)
        {
            var behavior = behaviors[index];
            var current = next;
            next = currentRequest => behavior.HandleAsync(currentRequest, current, cancellationToken);
        }

        return next(request);
    }
}
