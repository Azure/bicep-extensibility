// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;

namespace Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;

/// <summary>
/// Defines a handler for creating or updating a resource.
/// </summary>
public interface IResourceCreateOrUpdateHandler : IHandler<ResourceSpecification, OneOf<Resource, LongRunningOperation, ErrorResponse>>
{
    ///// <summary>
    ///// Executes the resource create or update operation.
    ///// </summary>
    ///// <param name="resourceSpecification">The specification of the resource to create or update.</param>
    ///// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    ///// <returns>A task that represents the asynchronous operation, containing the result of the create or update.</returns>
    ///// <exception cref="ErrorResponseException">Thrown when the operation encounters an error.</exception>
    //IHandler.Task<OneOf<Resource, LongRunningOperation, ErrorResponse>> HandleAsync(ResourceSpecification resourceSpecification, CancellationToken cancellationToken);
}
