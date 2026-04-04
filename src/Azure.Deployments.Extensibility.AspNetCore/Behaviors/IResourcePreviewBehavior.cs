// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;

namespace Azure.Deployments.Extensibility.AspNetCore.Behaviors;

/// <summary>
/// Delegate representing the next step in the resource preview behavior chain.
/// </summary>
public delegate Task<OneOf<ResourcePreview, ErrorResponse>> ResourcePreviewHandlerDelegate(ResourcePreviewSpecification request);

/// <summary>
/// Behavior for resource preview operations.
/// </summary>
public interface IResourcePreviewBehavior
    : IHandlerBehavior<ResourcePreviewSpecification, OneOf<ResourcePreview, ErrorResponse>>
{
    Task<OneOf<ResourcePreview, ErrorResponse>> IHandlerBehavior<ResourcePreviewSpecification, OneOf<ResourcePreview, ErrorResponse>>.HandleAsync(
        ResourcePreviewSpecification request,
        HandlerDelegate<ResourcePreviewSpecification, OneOf<ResourcePreview, ErrorResponse>> next,
        CancellationToken cancellationToken)
        => this.HandleAsync(request, req => next(req), cancellationToken);

    Task<OneOf<ResourcePreview, ErrorResponse>> HandleAsync(
        ResourcePreviewSpecification request,
        ResourcePreviewHandlerDelegate next,
        CancellationToken cancellationToken);
}
