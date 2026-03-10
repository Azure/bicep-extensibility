// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.AspNetCore.Pipeline;

/// <summary>
/// Represents the next step in the pipeline — either the next behavior or the inner handler.
/// </summary>
public delegate Task<TResponse> HandlerDelegate<TRequest, TResponse>(TRequest request);

public interface IHandlerPipelineBehavior<TRequest, TResponse>
{
    /// <summary>
    /// Executes the behavior. Call <paramref name="next"/> to continue the pipeline,
    /// or return early to short-circuit.
    /// </summary>
    Task<TResponse> HandleAsync(TRequest request, HandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken);
}

// Dedicated handler delegates — one per operation type.

public delegate Task<OneOf<ResourcePreview, ErrorResponse>> ResourcePreviewHandlerDelegate(ResourcePreviewSpecification request);

public delegate Task<OneOf<Resource, LongRunningOperation, ErrorResponse>> ResourceCreateOrUpdateHandlerDelegate(ResourceSpecification request);

public delegate Task<OneOf<Resource?, ErrorResponse>> ResourceGetHandlerDelegate(ResourceReference request);

public delegate Task<OneOf<Resource?, LongRunningOperation, ErrorResponse>> ResourceDeleteHandlerDelegate(ResourceReference request);

public delegate Task<OneOf<LongRunningOperation, ErrorResponse>> LongRunningOperationGetHandlerDelegate(JsonObject request);

/// <summary>
/// Pipeline behavior for resource preview operations.
/// </summary>
public interface IResourcePreviewPipelineBehavior
    : IHandlerPipelineBehavior<ResourcePreviewSpecification, OneOf<ResourcePreview, ErrorResponse>>
{
    Task<OneOf<ResourcePreview, ErrorResponse>> IHandlerPipelineBehavior<ResourcePreviewSpecification, OneOf<ResourcePreview, ErrorResponse>>.HandleAsync(
        ResourcePreviewSpecification request,
        HandlerDelegate<ResourcePreviewSpecification, OneOf<ResourcePreview, ErrorResponse>> next,
        CancellationToken cancellationToken)
        => HandleAsync(request, req => next(req), cancellationToken);

    Task<OneOf<ResourcePreview, ErrorResponse>> HandleAsync(
        ResourcePreviewSpecification request,
        ResourcePreviewHandlerDelegate next,
        CancellationToken cancellationToken);
}

/// <summary>
/// Pipeline behavior for resource create-or-update operations.
/// </summary>
public interface IResourceCreateOrUpdatePipelineBehavior
    : IHandlerPipelineBehavior<ResourceSpecification, OneOf<Resource, LongRunningOperation, ErrorResponse>>
{
    Task<OneOf<Resource, LongRunningOperation, ErrorResponse>> IHandlerPipelineBehavior<ResourceSpecification, OneOf<Resource, LongRunningOperation, ErrorResponse>>.HandleAsync(
        ResourceSpecification request,
        HandlerDelegate<ResourceSpecification, OneOf<Resource, LongRunningOperation, ErrorResponse>> next,
        CancellationToken cancellationToken)
        => HandleAsync(request, req => next(req), cancellationToken);

    Task<OneOf<Resource, LongRunningOperation, ErrorResponse>> HandleAsync(
        ResourceSpecification request,
        ResourceCreateOrUpdateHandlerDelegate next,
        CancellationToken cancellationToken);
}

/// <summary>
/// Pipeline behavior for resource get operations.
/// </summary>
public interface IResourceGetPipelineBehavior
    : IHandlerPipelineBehavior<ResourceReference, OneOf<Resource?, ErrorResponse>>
{
    Task<OneOf<Resource?, ErrorResponse>> IHandlerPipelineBehavior<ResourceReference, OneOf<Resource?, ErrorResponse>>.HandleAsync(
        ResourceReference request,
        HandlerDelegate<ResourceReference, OneOf<Resource?, ErrorResponse>> next,
        CancellationToken cancellationToken)
        => HandleAsync(request, req => next(req), cancellationToken);

    Task<OneOf<Resource?, ErrorResponse>> HandleAsync(
        ResourceReference request,
        ResourceGetHandlerDelegate next,
        CancellationToken cancellationToken);
}

/// <summary>
/// Pipeline behavior for resource delete operations.
/// </summary>
public interface IResourceDeletePipelineBehavior
    : IHandlerPipelineBehavior<ResourceReference, OneOf<Resource?, LongRunningOperation, ErrorResponse>>
{
    Task<OneOf<Resource?, LongRunningOperation, ErrorResponse>> IHandlerPipelineBehavior<ResourceReference, OneOf<Resource?, LongRunningOperation, ErrorResponse>>.HandleAsync(
        ResourceReference request,
        HandlerDelegate<ResourceReference, OneOf<Resource?, LongRunningOperation, ErrorResponse>> next,
        CancellationToken cancellationToken)
        => HandleAsync(request, req => next(req), cancellationToken);

    Task<OneOf<Resource?, LongRunningOperation, ErrorResponse>> HandleAsync(
        ResourceReference request,
        ResourceDeleteHandlerDelegate next,
        CancellationToken cancellationToken);
}

/// <summary>
/// Pipeline behavior for long-running operation get operations.
/// </summary>
public interface ILongRunningOperationGetPipelineBehavior
    : IHandlerPipelineBehavior<JsonObject, OneOf<LongRunningOperation, ErrorResponse>>
{
    Task<OneOf<LongRunningOperation, ErrorResponse>> IHandlerPipelineBehavior<JsonObject, OneOf<LongRunningOperation, ErrorResponse>>.HandleAsync(
        JsonObject request,
        HandlerDelegate<JsonObject, OneOf<LongRunningOperation, ErrorResponse>> next,
        CancellationToken cancellationToken)
        => HandleAsync(request, req => next(req), cancellationToken);

    Task<OneOf<LongRunningOperation, ErrorResponse>> HandleAsync(
        JsonObject request,
        LongRunningOperationGetHandlerDelegate next,
        CancellationToken cancellationToken);
}
