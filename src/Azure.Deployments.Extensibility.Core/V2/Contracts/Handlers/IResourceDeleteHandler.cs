// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts.Exceptions;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;

namespace Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;


/// <summary>
/// Defines a handler for deleting a resource.
/// </summary>
public interface IResourceDeleteHandler : IHandler
{
    /// <summary>
    /// Executes the resource delete operation.
    /// </summary>
    /// <param name="resourceReference">The reference identifying the resource to delete.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation, containing the result of the delete.</returns>
    /// <exception cref="ErrorResponseException">Thrown when the operation encounters an error.</exception>
    Task<OneOf<Resource?, LongRunningOperation, ErrorResponse>> HandleAsync(ResourceReference resourceReference, CancellationToken cancellationToken);
}
