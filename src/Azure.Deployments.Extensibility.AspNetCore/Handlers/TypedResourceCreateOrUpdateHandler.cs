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
/// Abstract base class for typed resource create-or-update handlers.
/// Converts between untyped and strongly-typed models before/after invoking the handler.
/// </summary>
/// <typeparam name="TProperties">The type representing the resource properties.</typeparam>
/// <typeparam name="TIdentifiers">The type representing the resource identifiers.</typeparam>
/// <typeparam name="TConfig">The type representing the extension configuration.</typeparam>
public abstract class TypedResourceCreateOrUpdateHandler<TProperties, TIdentifiers, TConfig> : TypedResourceOperationHandler<TProperties, TIdentifiers, TConfig>, IResourceCreateOrUpdateHandler
{
    protected TypedResourceCreateOrUpdateHandler(IOptions<JsonOptions> jsonOptions)
        : base(jsonOptions)
    {
    }

    public virtual async Task<OneOf<Resource, LongRunningOperation, ErrorResponse>> HandleAsync(ResourceSpecification request, CancellationToken cancellationToken)
    {
        var typedRequest = this.ToTypedResourceSpecification(request);
        var response = await this.HandleAsync(typedRequest, cancellationToken);

        return response.Match<OneOf<Resource, LongRunningOperation, ErrorResponse>>(
            typedResource => this.ToResource(typedResource),
            longRunningOperation => longRunningOperation,
            errorResponse => errorResponse);
    }

    protected abstract Task<OneOf<TypedResource, LongRunningOperation, ErrorResponse>> HandleAsync(TypedResourceSpecification request, CancellationToken cancellationToken);
}

/// <inheritdoc cref="TypedResourceCreateOrUpdateHandler{TProperties, TIdentifiers, TConfig}"/>
public abstract class TypedResourceCreateOrUpdateHandler<TProperties, TIdentifiers> : TypedResourceCreateOrUpdateHandler<TProperties, TIdentifiers, JsonObject?>
{
    protected TypedResourceCreateOrUpdateHandler(IOptions<JsonOptions> jsonOptions)
        : base(jsonOptions)
    {
    }
}
