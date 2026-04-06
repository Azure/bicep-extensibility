// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;

/// <summary>
/// Defines a handler for retrieving the status of a stepwise long-running operation.
/// An error response from this handler indicates a problem retrieving the status,
/// not a failure of the operation itself.
/// </summary>
/// <remarks>
/// See <see href="https://github.com/Azure/bicep-extensibility/blob/main/docs/v2/contract.md#long-running-operations">contract.md – Long-Running Operations</see>
/// and <see href="https://github.com/Azure/bicep-extensibility/blob/main/docs/v2/async-operations.md">async-operations.md</see>.
/// </remarks>
public interface ILongRunningOperationGetHandler : IHandler<JsonObject, OneOf<LongRunningOperation, ErrorResponse>>
{
}
