// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Behaviors;
using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using BlobPocExtension.Storage;
using System.Text.Json.Nodes;

namespace BlobPocExtension.Behaviors;

/// <summary>
/// Validates the required <c>accountName</c> once for every resource operation, short-circuiting with
/// an <see cref="ErrorResponse"/> before the handler runs. Reads <c>accountName</c> from properties for
/// create/preview and from identifiers for get/delete. The same <see cref="StorageValidation"/> rule
/// doubles as an SSRF guard on the account endpoint.
/// </summary>
public sealed class AccountNameValidationBehavior :
    IResourceCreateOrUpdateBehavior,
    IResourcePreviewBehavior,
    IResourceGetBehavior,
    IResourceDeleteBehavior
{
    Task<OneOf<Resource, LongRunningOperation, ErrorResponse>> IResourceCreateOrUpdateBehavior.HandleAsync(
        ResourceSpecification request, ResourceCreateOrUpdateHandlerDelegate next, CancellationToken cancellationToken)
    {
        if (ValidateProperties(request.Properties) is { } error)
        {
            return Task.FromResult<OneOf<Resource, LongRunningOperation, ErrorResponse>>(error);
        }

        return next(request);
    }

    Task<OneOf<ResourcePreview, ErrorResponse>> IResourcePreviewBehavior.HandleAsync(
        ResourcePreviewSpecification request, ResourcePreviewHandlerDelegate next, CancellationToken cancellationToken)
    {
        if (ValidateProperties(request.Properties) is { } error)
        {
            return Task.FromResult<OneOf<ResourcePreview, ErrorResponse>>(error);
        }

        return next(request);
    }

    Task<OneOf<Resource?, ErrorResponse>> IResourceGetBehavior.HandleAsync(
        ResourceReference request, ResourceGetHandlerDelegate next, CancellationToken cancellationToken)
    {
        if (ValidateIdentifiers(request.Identifiers) is { } error)
        {
            return Task.FromResult<OneOf<Resource?, ErrorResponse>>(error);
        }

        return next(request);
    }

    Task<OneOf<Resource?, LongRunningOperation, ErrorResponse>> IResourceDeleteBehavior.HandleAsync(
        ResourceReference request, ResourceDeleteHandlerDelegate next, CancellationToken cancellationToken)
    {
        if (ValidateIdentifiers(request.Identifiers) is { } error)
        {
            return Task.FromResult<OneOf<Resource?, LongRunningOperation, ErrorResponse>>(error);
        }

        return next(request);
    }

    private static ErrorResponse? ValidateProperties(JsonObject properties) =>
        StorageValidation.ValidateAccountName(properties["accountName"]?.GetValue<string>(), "/properties/accountName");

    private static ErrorResponse? ValidateIdentifiers(JsonObject identifiers) =>
        StorageValidation.ValidateAccountName(identifiers["accountName"]?.GetValue<string>(), "/identifiers/accountName");
}
