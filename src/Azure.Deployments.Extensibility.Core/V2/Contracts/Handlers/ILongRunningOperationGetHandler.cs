// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;

/// <summary>
/// Defines a handler for retrieving the status of a long-running operation.
/// </summary>
public interface ILongRunningOperationGetHandler : IHandler
{
    /// <summary>
    /// Retrieves the current status of a long-running operation.
    /// An error response should only be returned when there is an issue retrieving the operation
    /// status (e.g., network errors), NOT when the operation itself has failed. Operation failures
    /// should be reported via the operation's status field.
    /// </summary>
    /// <param name="operationHandle">An opaque handle that identifies the long-running operation.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation, containing the current status of the long-running operation or an error response.</returns>
    Task<OneOf<LongRunningOperation, ErrorResponse>> HandleAsync(JsonObject operationHandle, CancellationToken cancellationToken);
}
