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
/// V2 of the create/update handler. Adds "confidence" and "mood" to the fortune response.
/// Demonstrates version-based handler routing — requests with extensionVersion ≥ 2.0.0
/// are routed here instead of the v1 handler.
/// </summary>
[SupportedExtensionVersionRange(">=2.0.0")]
public class FortuneCreateOrUpdateHandlerV2 : TypedResourceCreateOrUpdateHandler
{
    private static readonly string[] Moods = ["Mystical", "Confident", "Uncertain", "Cosmic", "Playful"];

    private readonly FortuneStore store;

    public FortuneCreateOrUpdateHandlerV2(IHttpContextAccessor httpContextAccessor, FortuneStore store)
        : base(httpContextAccessor)
    {
        this.store = store;
    }

    protected override Task<OneOf<Resource, LongRunningOperation, ErrorResponse>> CreateOrUpdateResourceAsync(
        ResourceSpecification specification, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Creating/updating fortune resource (v2 handler).");

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

#pragma warning disable CA5394 // Random is fine here — confidence and mood don't need cryptographic security
        var confidence = Random.Shared.Next(1, 101);
        var mood = Moods[Random.Shared.Next(Moods.Length)];
#pragma warning restore CA5394

        var identifiers = new JsonObject { ["name"] = name };
        var properties = new JsonObject
        {
            ["name"] = name,
            ["question"] = question,
            ["fortune"] = fortune,
            ["confidence"] = confidence,
            ["mood"] = mood,
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

        Logger.LogInformation("Fortune '{Fortune}' (confidence: {Confidence}, mood: {Mood}) for '{Name}' — returning synchronously.", fortune, confidence, mood, name);
        this.store.StoreResource(key, resource);

        return Task.FromResult<OneOf<Resource, LongRunningOperation, ErrorResponse>>(resource);
    }
}
