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
/// Deletes a fortune resource (v2). Returns the deleted resource (including "confidence" and "mood") on success,
/// or null (204) if it didn't exist.
/// </summary>
public class FortuneDeleteHandler
    : TypedResourceDeleteHandler<FortunePropertiesV2, FortuneIdentifiers>
{
    private readonly FortuneStore store;

    public FortuneDeleteHandler(
        IOptions<JsonOptions> jsonOptions,
        FortuneStore store)
        : base(jsonOptions)
    {
        this.store = store;
    }

    protected override Task<OneOf<TypedResource?, LongRunningOperation, ErrorResponse>> HandleAsync(
        TypedResourceReference request, CancellationToken cancellationToken)
    {
        var key = FortuneStore.GetResourceKey(request.Type, request.Identifiers.Name);
        var removed = this.store.RemoveResource(key);

        // Returning null signals 204 No Content (resource already deleted or never existed).
        TypedResource? result = removed is not null ? this.ToTypedResource(removed) : null;

        return Task.FromResult<OneOf<TypedResource?, LongRunningOperation, ErrorResponse>>(result);
    }
}
