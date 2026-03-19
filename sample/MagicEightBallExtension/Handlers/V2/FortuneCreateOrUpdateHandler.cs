// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Handlers;
using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using MagicEightBallExtension.Data;
using MagicEightBallExtension.Models;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MagicEightBallExtension.Handlers.V2;

/// <summary>
/// Creates or updates a fortune resource (v2). Adds "confidence" and "mood" to the fortune response.
/// Demonstrates version-based handler routing — requests with extensionVersion >= 2.0.0
/// are routed here instead of the v1 handler.
/// </summary>
public class FortuneCreateOrUpdateHandler
    : TypedResourceCreateOrUpdateHandler<FortunePropertiesV2, FortuneIdentifiers>
{
    private readonly FortuneStore store;
    private readonly ILogger<FortuneCreateOrUpdateHandler> logger;

    private static readonly string[] Moods = ["Mystical", "Confident", "Uncertain", "Cosmic", "Playful"];

    public FortuneCreateOrUpdateHandler(
        IOptions<JsonOptions> jsonOptions,
        FortuneStore store,
        ILogger<FortuneCreateOrUpdateHandler> logger)
        : base(jsonOptions)
    {
        this.store = store;
        this.logger = logger;
    }

    protected override Task<OneOf<TypedResource, LongRunningOperation, ErrorResponse>> HandleAsync(
        TypedResourceSpecification request, CancellationToken cancellationToken)
    {
        var fortune = this.store.GetRandomFortune();

#pragma warning disable CA5394 // Random is fine here — confidence and mood don't need cryptographic security
        var confidence = Random.Shared.Next(1, 101);
        var mood = Moods[Random.Shared.Next(Moods.Length)];
#pragma warning restore CA5394

        var resource = new TypedResource
        {
            Type = request.Type,
            ApiVersion = request.ApiVersion,
            Identifiers = new FortuneIdentifiers { Name = request.Properties.Name },
            Properties = request.Properties with
            {
                Fortune = fortune,
                Confidence = confidence,
                Mood = mood,
                AnsweredAt = DateTimeOffset.UtcNow.ToString("o"),
            },
            Config = request.Config,
            ConfigId = request.Config is not null ? "static-config-id" : null,
        };

        var key = FortuneStore.GetResourceKey(request.Type, resource.Identifiers.Name);

        if (FortuneStore.RequiresCosmicContemplation(fortune))
        {
            this.logger.LogInformation("Fortune requires cosmic contemplation — starting LRO for '{Name}'.", request.Properties.Name);
            var operationId = this.store.CreatePendingOperation(this.ToResource(resource), key);

            return Task.FromResult<OneOf<TypedResource, LongRunningOperation, ErrorResponse>>(
                new LongRunningOperation
                {
                    Status = "CosmicContemplation",
                    RetryAfterSeconds = 3,
                    OperationHandle = new JsonObject { ["operationId"] = operationId },
                });
        }

        this.store.StoreResource(key, this.ToResource(resource));

        return Task.FromResult<OneOf<TypedResource, LongRunningOperation, ErrorResponse>>(resource);
    }
}
