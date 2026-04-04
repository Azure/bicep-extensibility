// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Deployments.Extensibility.AspNetCore.Behaviors;

/// <summary>
/// Represents the next step in the behavior chain — either the next behavior or the inner handler.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public delegate Task<TResponse> HandlerDelegate<TRequest, TResponse>(TRequest request);

/// <summary>
/// Defines a behavior that wraps a handler invocation, enabling cross-cutting concerns such as
/// validation, logging, error handling, or authorization.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public interface IHandlerBehavior<TRequest, TResponse>
{
    /// <summary>
    /// Executes the behavior. Call <paramref name="next"/> to continue the chain,
    /// or return early to short-circuit.
    /// </summary>
    Task<TResponse> HandleAsync(TRequest request, HandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken);
}
