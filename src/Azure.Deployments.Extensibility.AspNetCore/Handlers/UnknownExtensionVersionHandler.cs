// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;

namespace Azure.Deployments.Extensibility.AspNetCore.Handlers;

/// <summary>
/// A fallback handler returned by <see cref="HandlerRegistry"/> when the requested
/// extension version does not match any registered version range.
/// </summary>
internal sealed class UnknownExtensionVersionHandler :
    IResourcePreviewHandler,
    IResourceCreateOrUpdateHandler,
    IResourceGetHandler,
    IResourceDeleteHandler
{
    private readonly string extensionVersion;

    internal UnknownExtensionVersionHandler(string extensionVersion)
    {
        this.extensionVersion = extensionVersion;
    }

    private ErrorResponse CreateErrorResponse() => new(
        new Error("UnsupportedExtensionVersion", $"No handler found for extension version '{this.extensionVersion}'."));

    Task<OneOf<ResourcePreview, ErrorResponse>> IHandler<ResourcePreviewSpecification, OneOf<ResourcePreview, ErrorResponse>>.HandleAsync(
        ResourcePreviewSpecification request, CancellationToken cancellationToken)
    {
        return Task.FromResult<OneOf<ResourcePreview, ErrorResponse>>(this.CreateErrorResponse());
    }

    Task<OneOf<Resource, LongRunningOperation, ErrorResponse>> IHandler<ResourceSpecification, OneOf<Resource, LongRunningOperation, ErrorResponse>>.HandleAsync(
        ResourceSpecification request, CancellationToken cancellationToken)
    {
        return Task.FromResult<OneOf<Resource, LongRunningOperation, ErrorResponse>>(this.CreateErrorResponse());
    }

    Task<OneOf<Resource?, ErrorResponse>> IHandler<ResourceReference, OneOf<Resource?, ErrorResponse>>.HandleAsync(
        ResourceReference request, CancellationToken cancellationToken)
    {
        return Task.FromResult<OneOf<Resource?, ErrorResponse>>(this.CreateErrorResponse());
    }

    Task<OneOf<Resource?, LongRunningOperation, ErrorResponse>> IHandler<ResourceReference, OneOf<Resource?, LongRunningOperation, ErrorResponse>>.HandleAsync(
        ResourceReference request, CancellationToken cancellationToken)
    {
        return Task.FromResult<OneOf<Resource?, LongRunningOperation, ErrorResponse>>(this.CreateErrorResponse());
    }
}
