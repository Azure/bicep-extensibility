// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;

namespace Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;

/// <summary>
/// Defines a handler for retrieving a resource.
/// </summary>
public interface IResourceGetHandler : IHandler<ResourceReference, OneOf<Resource?, ErrorResponse>>
{
    ///// <summary>
    ///// Executes the resource get operation.
    ///// </summary>
    ///// <param name="resourceReference">The reference identifying the resource to retrieve.</param>
    ///// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    ///// <returns>A task that represents the asynchronous operation, containing the result of the get.</returns>
    ///// <exception cref="ErrorResponseException">Thrown when the operation encounters an error.</exception>
    //Task<OneOf<Resource?, ErrorResponse>> HandleAsync(ResourceReference resourceReference, CancellationToken cancellationToken);
}
