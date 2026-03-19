// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;
using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Exceptions;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace Azure.Deployments.Extensibility.AspNetCore.Handlers;

public abstract class TypedResourceGetHandler<TProperties, TIdentifiers, TConfig> : TypedResourceOperationHandler<TProperties, TIdentifiers, TConfig>,  IResourceGetHandler
{
    public TypedResourceGetHandler(IOptions<JsonOptions> jsonOptions)
        : base(jsonOptions)
    {
    }
    
    public async Task<OneOf<Resource?, ErrorResponse>> HandleAsync(ResourceReference request, CancellationToken cancellationToken)
    {
        var typedRequest = this.ToTypedResourceReference(request);
        var response = await this.HandleAsync(typedRequest, cancellationToken);

        return response.Match<OneOf<Resource?, ErrorResponse>>(
            typedResource => this.ToNullableResource(typedResource),
            errorResponse => errorResponse);
    }
    
    protected abstract Task<OneOf<TypedResource?, ErrorResponse>> HandleAsync(TypedResourceReference request, CancellationToken cancellationToken);
}

public abstract class TypedResourceGetHandler<TProperties, TIdentifiers> : TypedResourceGetHandler<TProperties, TIdentifiers, JsonObject>
{
    public TypedResourceGetHandler(IOptions<JsonOptions> jsonOptions)
        : base(jsonOptions)
    {
    }
}
