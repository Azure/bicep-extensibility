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
/// Retrieves a fortune resource by its identifiers.
/// Returns null (maps to 404) if the fortune does not exist.
/// </summary>
[SupportedExtensionVersionRange(">=1.0.0")]
public class FortuneGetHandler : TypedResourceGetHttpHandler
{
    private readonly FortuneStore store;

    public FortuneGetHandler(IHttpContextAccessor httpContextAccessor, FortuneStore store)
        : base(httpContextAccessor)
    {
        this.store = store;
    }

    protected override Task<OneOf<Resource?, ErrorResponse>> GetResourceAsync(
        ResourceReference reference, CancellationToken cancellationToken)
    {
        var key = FortuneStore.GetResourceKey(reference.Type, reference.Identifiers);
        var resource = this.store.TryGetResource(key);

        Logger.LogInformation("Get fortune '{Key}': {Result}.", key, resource is not null ? "found" : "not found");

        // Returning null signals 404 Not Found to the framework.
        return Task.FromResult<OneOf<Resource?, ErrorResponse>>(resource);
    }
}
