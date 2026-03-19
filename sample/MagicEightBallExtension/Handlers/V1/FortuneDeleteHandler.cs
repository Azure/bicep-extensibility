// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Handlers;
using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using MagicEightBallExtension.Data;
using MagicEightBallExtension.Models;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace MagicEightBallExtension.Handlers.V1;

/// <summary>
/// Deletes a fortune resource. Returns the deleted resource on success, or null (204) if it didn't exist.
/// </summary>
public class FortuneDeleteHandler
    : TypedResourceDeleteHandler<FortuneProperties, FortuneIdentifiers>
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
