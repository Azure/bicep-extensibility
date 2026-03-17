// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;

/// <summary>
/// Defines a handler for retrieving the status of a long-running operation.
/// </summary>
public interface ILongRunningOperationGetHandler : IHandler<JsonObject, OneOf<LongRunningOperation, ErrorResponse>>
{
}
