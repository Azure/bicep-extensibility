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

public abstract class TypedResourceCreateOrUpdateHandler<TProperties, TIdentifiers> : TypedResourceCreateOrUpdateHandler<TProperties, TIdentifiers, JsonObject?>
{
    protected TypedResourceCreateOrUpdateHandler(IOptions<JsonOptions> jsonOptions)
        : base(jsonOptions)
    {
    }
}
