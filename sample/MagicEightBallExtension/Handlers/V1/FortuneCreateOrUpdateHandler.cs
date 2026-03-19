// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Handlers;
using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using MagicEightBallExtension.Data;
using MagicEightBallExtension.Models;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using System.Text.Json.Nodes;

namespace MagicEightBallExtension.Handlers.V1;

/// <summary>
/// Creates or updates a fortune resource (v1). Shakes the Magic 8-Ball and returns an answer.
/// If the fortune requires "cosmic contemplation", returns a 202 Accepted with an LRO.
/// </summary>
public class FortuneCreateOrUpdateHandler
    : TypedResourceCreateOrUpdateHandler<FortuneProperties, FortuneIdentifiers>
{
    private readonly FortuneStore store;
    private readonly ILogger<FortuneCreateOrUpdateHandler> logger;

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

        var resource = new TypedResource
        {
            Type = request.Type,
            ApiVersion = request.ApiVersion,
            Identifiers = new FortuneIdentifiers { Name = request.Properties.Name },
            Properties = request.Properties with
            {
                Fortune = fortune,
                AnsweredAt = DateTimeOffset.UtcNow.ToString("o"),
            },
            Config = request.Config,
            ConfigId = request.Config is not null ? "static-config-id" : null,
        };

        var key = FortuneStore.GetResourceKey(request.Type, resource.Identifiers.Name);

        // If the fortune requires cosmic contemplation, simulate a long-running operation.
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
