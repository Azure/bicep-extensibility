// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;

namespace Azure.Deployments.Extensibility.AspNetCore.Handlers;

/// <summary>
/// A fallback handler returned by <see cref="HandlerRegistry"/> when the requested
/// resource type does not match any registered resource-type-specific or generic handler.
/// </summary>
internal sealed class UnknownResourceTypeHandler :
    IResourcePreviewHandler,
    IResourceCreateOrUpdateHandler,
    IResourceGetHandler,
    IResourceDeleteHandler
{
    private static ErrorResponse CreateErrorResponse(string? resourceType) => new(
        new Error("UnsupportedResourceType", $"The resource type '{resourceType}' is not supported."));

    Task<OneOf<ResourcePreview, ErrorResponse>> IHandler<ResourcePreviewSpecification, OneOf<ResourcePreview, ErrorResponse>>.HandleAsync(
        ResourcePreviewSpecification request, CancellationToken cancellationToken)
    {
        return Task.FromResult<OneOf<ResourcePreview, ErrorResponse>>(CreateErrorResponse(request.Type));
    }

    Task<OneOf<Resource, LongRunningOperation, ErrorResponse>> IHandler<ResourceSpecification, OneOf<Resource, LongRunningOperation, ErrorResponse>>.HandleAsync(
        ResourceSpecification request, CancellationToken cancellationToken)
    {
        return Task.FromResult<OneOf<Resource, LongRunningOperation, ErrorResponse>>(CreateErrorResponse(request.Type));
    }

    Task<OneOf<Resource?, ErrorResponse>> IHandler<ResourceReference, OneOf<Resource?, ErrorResponse>>.HandleAsync(
        ResourceReference request, CancellationToken cancellationToken)
    {
        return Task.FromResult<OneOf<Resource?, ErrorResponse>>(CreateErrorResponse(request.Type));
    }

    Task<OneOf<Resource?, LongRunningOperation, ErrorResponse>> IHandler<ResourceReference, OneOf<Resource?, LongRunningOperation, ErrorResponse>>.HandleAsync(
        ResourceReference request, CancellationToken cancellationToken)
    {
        return Task.FromResult<OneOf<Resource?, LongRunningOperation, ErrorResponse>>(CreateErrorResponse(request.Type));
    }
}
