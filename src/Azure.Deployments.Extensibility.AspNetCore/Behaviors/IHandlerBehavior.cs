// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Deployments.Extensibility.AspNetCore.Behaviors;

/// <summary>
/// Represents the next step in the decorator chain — either the next decorator or the inner handler.
/// </summary>
public delegate Task<TResponse> HandlerDelegate<TRequest, TResponse>(TRequest request);

public interface IHandlerBehavior<TRequest, TResponse>
{
    /// <summary>
    /// Executes the behavior. Call <paramref name="next"/> to continue the chain,
    /// or return early to short-circuit.
    /// </summary>
    Task<TResponse> HandleAsync(TRequest request, HandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken);
}
