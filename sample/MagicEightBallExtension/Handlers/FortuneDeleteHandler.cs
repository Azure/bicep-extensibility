// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Handlers;
using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using MagicEightBallExtension.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace MagicEightBallExtension.Handlers;

/// <summary>
/// Deletes a fortune resource. Returns the deleted resource on success, or null (204) if it didn't exist.
/// </summary>
[SupportedExtensionVersionRange(">=1.0.0")]
public class FortuneDeleteHandler : TypedResourceDeleteHandler
{
    private readonly FortuneStore store;

    public FortuneDeleteHandler(IHttpContextAccessor httpContextAccessor, FortuneStore store)
        : base(httpContextAccessor)
    {
        this.store = store;
    }

    protected override Task<OneOf<Resource?, LongRunningOperation, ErrorResponse>> DeleteResourceAsync(
        ResourceReference reference, CancellationToken cancellationToken)
    {
        var key = FortuneStore.GetResourceKey(reference.Type, reference.Identifiers);
        var removed = this.store.RemoveResource(key);

        Logger.LogInformation("Delete fortune '{Key}': {Result}.", key, removed is not null ? "removed" : "not found");

        // Returning null signals 204 No Content (resource already deleted or never existed).
        return Task.FromResult<OneOf<Resource?, LongRunningOperation, ErrorResponse>>(removed);
    }
}
