// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Core.V2.Contracts.Models;

/// <summary>
/// Represents the result of a long-running operation, including its current status
/// and optional retry, handle, and error information.
/// </summary>
public record LongRunningOperation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LongRunningOperation"/> record.
    /// </summary>
    public LongRunningOperation()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LongRunningOperation"/> record
    /// with the specified properties.
    /// </summary>
    /// <param name="status">The current status of the operation.</param>
    /// <param name="retryAfterSeconds">The recommended polling interval in seconds.</param>
    /// <param name="operationHandle">An opaque handle used to track the operation.</param>
    /// <param name="error">The error information if the operation failed.</param>
    [SetsRequiredMembers]
    public LongRunningOperation(
        string status,
        int? retryAfterSeconds = null,
        JsonObject? operationHandle = null,
        Error? error = null)
    {
        this.Status = status;
        this.RetryAfterSeconds = retryAfterSeconds;
        this.OperationHandle = operationHandle;
        this.Error = error;
    }

    /// <summary>
    /// The current status of the operation (e.g., "Succeeded", "Failed", "Canceled", or a non-terminal status).
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// The recommended number of seconds to wait before polling the operation status again.
    /// </summary>
    public int? RetryAfterSeconds { get; init; }

    /// <summary>
    /// An opaque handle used to track the operation. Must be specified if <see cref="Status"/> is not
    /// terminal (i.e., not "Succeeded", "Failed", or "Canceled").
    /// </summary>
    public JsonObject? OperationHandle { get; init; }

    /// <summary>
    /// The error information if the operation failed.
    /// </summary>
    public Error? Error { get; init; }
}
