// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Semver;

namespace Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;

/// <summary>
/// Non-generic marker interface for all handler types.
/// </summary>
public interface IHandler
{
}


/// <summary>
/// Base interface for all handlers in the extensibility API.
/// </summary>
/// <typeparam name="TRequest">The request type accepted by the handler.</typeparam>
/// <typeparam name="TResponse">The response type produced by the handler.</typeparam>
public interface IHandler<TRequest, TResponse> : IHandler
{
    /// <summary>
    /// Handle the specified request.
    /// </summary>
    /// <param name="request">The request to handle.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The response produced by the handler.</returns>
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken);
}
