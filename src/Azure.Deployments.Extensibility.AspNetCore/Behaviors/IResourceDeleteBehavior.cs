// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;

namespace Azure.Deployments.Extensibility.AspNetCore.Behaviors;

/// <summary>
/// Delegate representing the next step in the resource delete behavior chain.
/// </summary>
public delegate Task<OneOf<Resource?, LongRunningOperation, ErrorResponse>> ResourceDeleteHandlerDelegate(ResourceReference request);

/// <summary>
/// Behavior for resource delete operations.
/// </summary>
public interface IResourceDeleteBehavior
    : IHandlerBehavior<ResourceReference, OneOf<Resource?, LongRunningOperation, ErrorResponse>>
{
    Task<OneOf<Resource?, LongRunningOperation, ErrorResponse>> IHandlerBehavior<ResourceReference, OneOf<Resource?, LongRunningOperation, ErrorResponse>>.HandleAsync(
        ResourceReference request,
        HandlerDelegate<ResourceReference, OneOf<Resource?, LongRunningOperation, ErrorResponse>> next,
        CancellationToken cancellationToken)
        => this.HandleAsync(request, req => next(req), cancellationToken);

    Task<OneOf<Resource?, LongRunningOperation, ErrorResponse>> HandleAsync(
        ResourceReference request,
        ResourceDeleteHandlerDelegate next,
        CancellationToken cancellationToken);
}
