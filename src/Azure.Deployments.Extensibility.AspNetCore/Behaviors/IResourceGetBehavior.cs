// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;

namespace Azure.Deployments.Extensibility.AspNetCore.Behaviors;

/// <summary>
/// Delegate representing the next step in the resource get behavior chain.
/// </summary>
public delegate Task<OneOf<Resource?, ErrorResponse>> ResourceGetHandlerDelegate(ResourceReference request);

/// <summary>
/// Behavior for resource get operations.
/// </summary>
public interface IResourceGetBehavior
    : IHandlerBehavior<ResourceReference, OneOf<Resource?, ErrorResponse>>
{
    Task<OneOf<Resource?, ErrorResponse>> IHandlerBehavior<ResourceReference, OneOf<Resource?, ErrorResponse>>.HandleAsync(
        ResourceReference request,
        HandlerDelegate<ResourceReference, OneOf<Resource?, ErrorResponse>> next,
        CancellationToken cancellationToken)
        => this.HandleAsync(request, req => next(req), cancellationToken);

    Task<OneOf<Resource?, ErrorResponse>> HandleAsync(
        ResourceReference request,
        ResourceGetHandlerDelegate next,
        CancellationToken cancellationToken);
}
