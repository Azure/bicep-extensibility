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

public abstract class TypedResourceDeleteHandler<TProperties, TIdentifiers, TConfig> : TypedResourceOperationHandler<TProperties, TIdentifiers, TConfig>, IResourceDeleteHandler
{
    protected TypedResourceDeleteHandler(IOptions<JsonOptions> jsonOptions)
        : base(jsonOptions)
    {
    }

    public async Task<OneOf<Resource?, LongRunningOperation, ErrorResponse>> HandleAsync(ResourceReference resourceReference, CancellationToken cancellationToken)
    {
        var typedRequest = this.ToTypedResourceReference(resourceReference);
        var response = await this.HandleAsync(typedRequest, cancellationToken);

        return response.Match<OneOf<Resource?, LongRunningOperation, ErrorResponse>>(
            typedResource => this.ToNullableResource(typedResource),
            longRunningOperation => longRunningOperation,
            errorResponse => errorResponse);
    }

    protected abstract Task<OneOf<TypedResource?, LongRunningOperation, ErrorResponse>> HandleAsync(TypedResourceReference typedRequest, CancellationToken cancellationToken);
}

public abstract class TypedResourceDeleteHandler<TProperties, TIdentifiers> : TypedResourceDeleteHandler<TProperties, TIdentifiers, JsonObject?>
{
    protected TypedResourceDeleteHandler(IOptions<JsonOptions> jsonOptions)
        : base(jsonOptions)
    {
    }
}
