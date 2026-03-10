// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Handlers;
using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using MagicEightBallExtension.Data;
using Microsoft.Extensions.Logging;
using System.Text.Json.Nodes;

namespace MagicEightBallExtension.Handlers;

/// <summary>
/// Creates or updates a fortune resource. Shakes the Magic 8-Ball and returns an answer.
/// If the fortune requires "cosmic contemplation", returns a 202 Accepted with an LRO.
/// </summary>
[SupportedExtensionVersionRange(">=1.0.0 <2.0.0")]
public class FortuneCreateOrUpdateHandler : TypedResourceCreateOrUpdateHandler
{
    private readonly FortuneStore store;

    public FortuneCreateOrUpdateHandler(IHttpContextAccessor httpContextAccessor, FortuneStore store)
        : base(httpContextAccessor)
    {
        this.store = store;
    }

    protected override Task<OneOf<Resource, LongRunningOperation, ErrorResponse>> CreateOrUpdateResourceAsync(
        ResourceSpecification specification, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Creating/updating fortune resource (v1 handler).");

        var name = specification.Properties["name"]?.GetValue<string>();

        if (string.IsNullOrWhiteSpace(name))
        {
            return Task.FromResult<OneOf<Resource, LongRunningOperation, ErrorResponse>>(
                new ErrorResponse(new Error
                {
                    Code = "MissingRequiredProperty",
                    Message = "The 'name' property is required.",
                    Target = Json.Pointer.JsonPointer.Parse("/properties/name"),
                }));
        }

        var question = specification.Properties["question"]?.GetValue<string>() ?? "Will I be lucky today?";
        var fortune = this.store.GetRandomFortune();

        var identifiers = new JsonObject { ["name"] = name };
        var properties = new JsonObject
        {
            ["name"] = name,
            ["question"] = question,
            ["fortune"] = fortune,
            ["answeredAt"] = DateTimeOffset.UtcNow.ToString("o"),
        };

        var resource = new Resource
        {
            Type = specification.Type,
            ApiVersion = specification.ApiVersion,
            Identifiers = identifiers,
            Properties = properties,
            Config = specification.Config?.DeepClone()?.AsObject(),
            ConfigId = specification.Config is not null ? "static-config-id" : null,
        };

        var key = FortuneStore.GetResourceKey(specification.Type, identifiers);

        // If the fortune requires cosmic contemplation, simulate a long-running operation.
        if (FortuneStore.RequiresCosmicContemplation(fortune))
        {
            Logger.LogInformation("Fortune '{Fortune}' requires cosmic contemplation — starting LRO for '{Name}'.", fortune, name);
            var operationId = this.store.CreatePendingOperation(resource, key);

            return Task.FromResult<OneOf<Resource, LongRunningOperation, ErrorResponse>>(
                new LongRunningOperation
                {
                    Status = "CosmicContemplation",
                    RetryAfterSeconds = 3,
                    OperationHandle = new JsonObject { ["operationId"] = operationId },
                });
        }

        // Synchronous completion — store and return the resource.
        Logger.LogInformation("Fortune '{Fortune}' for '{Name}' — returning synchronously.", fortune, name);
        this.store.StoreResource(key, resource);

        return Task.FromResult<OneOf<Resource, LongRunningOperation, ErrorResponse>>(resource);
    }
}
