// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using MagicEightBallExtension.Data;
using Microsoft.Extensions.Logging;
using System.Text.Json.Nodes;

namespace MagicEightBallExtension.Handlers;

/// <summary>
/// Retrieves the status of a long-running "cosmic contemplation" operation.
/// Simulates a delay: the operation completes after 5 seconds.
/// </summary>
public class FortuneLongRunningOperationGetHandler : ILongRunningOperationGetHandler
{
    private readonly FortuneStore store;
    private readonly ILogger<FortuneLongRunningOperationGetHandler> logger;

    public FortuneLongRunningOperationGetHandler(FortuneStore store, ILogger<FortuneLongRunningOperationGetHandler> logger)
    {
        this.store = store;
        this.logger = logger;
    }

    private static readonly TimeSpan CosmicContemplationDuration = TimeSpan.FromSeconds(5);

    public Task<OneOf<LongRunningOperation, ErrorResponse>> HandleAsync(
        JsonObject operationHandle, CancellationToken cancellationToken)
    {
        var operationId = operationHandle["operationId"]?.GetValue<string>();

        if (operationId is null)
        {
            return Task.FromResult<OneOf<LongRunningOperation, ErrorResponse>>(
                new ErrorResponse(new Error
                {
                    Code = "InvalidOperationHandle",
                    Message = "The operation handle must contain an 'operationId' property.",
                }));
        }

        var pending = this.store.TryGetPendingOperation(operationId);

        if (pending is null)
        {
            this.logger.LogInformation("LRO '{OperationId}': already completed or not found.", operationId);
            return Task.FromResult<OneOf<LongRunningOperation, ErrorResponse>>(
                new LongRunningOperation { Status = "Succeeded" });
        }

        var elapsed = DateTimeOffset.UtcNow - pending.CreatedAt;

        if (elapsed >= CosmicContemplationDuration)
        {
            this.logger.LogInformation("LRO '{OperationId}': cosmic contemplation complete after {Elapsed}.", operationId, elapsed);
            this.store.CompletePendingOperation(operationId);

            return Task.FromResult<OneOf<LongRunningOperation, ErrorResponse>>(
                new LongRunningOperation { Status = "Succeeded" });
        }

        this.logger.LogInformation("LRO '{OperationId}': still contemplating ({Elapsed} elapsed).", operationId, elapsed);

        // Still contemplating — return a non-terminal status with the same handle.
        return Task.FromResult<OneOf<LongRunningOperation, ErrorResponse>>(
            new LongRunningOperation
            {
                Status = "CosmicContemplation",
                RetryAfterSeconds = 3,
                OperationHandle = new JsonObject { ["operationId"] = operationId },
            });
    }
}
