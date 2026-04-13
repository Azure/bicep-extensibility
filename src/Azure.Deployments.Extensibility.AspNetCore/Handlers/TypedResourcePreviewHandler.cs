// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Exceptions;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.AspNetCore.Handlers;

/// <summary>
/// Abstract base class for HTTP-based resource preview handlers.
/// Provides model validation and <see cref="ErrorResponseException"/> handling.
/// </summary>
public abstract class TypedResourcePreviewHandler<TProperties, TIdentifiers, TConfig> : TypedResourceOperationHandler<TProperties, TIdentifiers, TConfig>, IResourcePreviewHandler
{
    protected TypedResourcePreviewHandler(IOptions<JsonOptions> jsonOptions)
        : base(jsonOptions)
    {
    }

    public virtual async Task<OneOf<ResourcePreview, ErrorResponse>> HandleAsync(ResourcePreviewSpecification request, CancellationToken cancellationToken)
    {
        var typedRequest = this.ToTypedResourcePreviewSpecification(request);
        var response = await this.HandleAsync(typedRequest, cancellationToken);

        return response.Match<OneOf<ResourcePreview, ErrorResponse>>(
            typedResourcePreview => this.ToResourcePreview(typedResourcePreview),
            errorResponse => errorResponse);
    }

    protected abstract Task<OneOf<TypedResourcePreview, ErrorResponse>> HandleAsync(TypedResourcePreviewSpecification request, CancellationToken cancellationToken);
}

/// <inheritdoc cref="TypedResourcePreviewHandler{TProperties, TIdentifiers, TConfig}"/>
public abstract class TypedResourcePreviewHandler<TProperties, TIdentifiers> : TypedResourcePreviewHandler<TProperties, TIdentifiers, JsonObject?>
{
    protected TypedResourcePreviewHandler(IOptions<JsonOptions> jsonOptions)
        : base(jsonOptions)
    {
    }
}
