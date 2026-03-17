// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Handlers;
using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using MagicEightBallExtension.Data;
using MagicEightBallExtension.Models;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace MagicEightBallExtension.Handlers.V2;

/// <summary>
/// Retrieves a fortune resource (v2) by its identifiers, including the v2 "confidence" and "mood" fields.
/// Returns null (maps to 404) if the fortune does not exist.
/// </summary>
public class FortuneGetHandler
    : TypedResourceGetHandler<FortunePropertiesV2, FortuneIdentifiers>
{
    private readonly FortuneStore store;

    public FortuneGetHandler(
        IOptions<JsonOptions> jsonOptions,
        FortuneStore store)
        : base(jsonOptions)
    {
        this.store = store;
    }

    protected override Task<OneOf<TypedResource?, ErrorResponse>> HandleAsync(
        TypedResourceReference request, CancellationToken cancellationToken)
    {
        var key = FortuneStore.GetResourceKey(request.Type, request.Identifiers.Name);
        var resource = this.store.TryGetResource(key);

        // Returning null signals 404 Not Found to the framework.
        TypedResource? result = resource is not null ? this.ToTypedResource(resource) : null;

        return Task.FromResult<OneOf<TypedResource?, ErrorResponse>>(result);
    }
}
