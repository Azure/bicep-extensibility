// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;

namespace Azure.Deployments.Extensibility.AspNetCore.Behaviors;

/// <summary>
/// Delegate representing the next step in the resource create-or-update behavior chain.
/// </summary>
public delegate Task<OneOf<Resource, LongRunningOperation, ErrorResponse>> ResourceCreateOrUpdateHandlerDelegate(ResourceSpecification request);

/// <summary>
/// Behavior for resource create-or-update operations.
/// </summary>
public interface IResourceCreateOrUpdateBehavior
    : IHandlerBehavior<ResourceSpecification, OneOf<Resource, LongRunningOperation, ErrorResponse>>
{
    Task<OneOf<Resource, LongRunningOperation, ErrorResponse>> IHandlerBehavior<ResourceSpecification, OneOf<Resource, LongRunningOperation, ErrorResponse>>.HandleAsync(
        ResourceSpecification request,
        HandlerDelegate<ResourceSpecification, OneOf<Resource, LongRunningOperation, ErrorResponse>> next,
        CancellationToken cancellationToken)
        => this.HandleAsync(request, req => next(req), cancellationToken);

    Task<OneOf<Resource, LongRunningOperation, ErrorResponse>> HandleAsync(
        ResourceSpecification request,
        ResourceCreateOrUpdateHandlerDelegate next,
        CancellationToken cancellationToken);
}
