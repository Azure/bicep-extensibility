// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.AspNetCore.Behaviors;

public delegate Task<OneOf<LongRunningOperation, ErrorResponse>> LongRunningOperationGetHandlerDelegate(JsonObject request);

/// <summary>
/// Behavior for long-running operation get operations.
/// </summary>
public interface ILongRunningOperationGetBehavior
    : IHandlerBehavior<JsonObject, OneOf<LongRunningOperation, ErrorResponse>>
{
    Task<OneOf<LongRunningOperation, ErrorResponse>> IHandlerBehavior<JsonObject, OneOf<LongRunningOperation, ErrorResponse>>.HandleAsync(
        JsonObject request,
        HandlerDelegate<JsonObject, OneOf<LongRunningOperation, ErrorResponse>> next,
        CancellationToken cancellationToken)
        => this.HandleAsync(request, req => next(req), cancellationToken);

    Task<OneOf<LongRunningOperation, ErrorResponse>> HandleAsync(
        JsonObject request,
        LongRunningOperationGetHandlerDelegate next,
        CancellationToken cancellationToken);
}
