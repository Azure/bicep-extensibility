// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using System.Text.Json.Nodes;

namespace BlobPocExtension.Handlers;

/// <summary>
/// Returns the status of a long-running blob operation referenced by its operation handle.
/// </summary>
public sealed class BlobLongRunningOperationGetHandler : ILongRunningOperationGetHandler
{
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

        // TODO: replace with a real poll against the underlying blob operation.
        return Task.FromResult<OneOf<LongRunningOperation, ErrorResponse>>(
            new LongRunningOperation { Status = "Succeeded" });
    }
}
