// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts.Exceptions;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;

namespace Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;

/// <summary>
/// Defines a handler for previewing a resource deployment.
/// </summary>
public interface IResourcePreviewHandler : IHandler<ResourcePreviewSpecification, OneOf<ResourcePreview, ErrorResponse>>
{
    ///// <summary>
    ///// Executes the resource preview operation.
    ///// </summary>
    ///// <param name="resourcePreviewSpecification">The specification of the resource to preview.</param>
    ///// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    ///// <returns>A task that represents the asynchronous operation, containing the result of the preview.</returns>
    ///// <exception cref="ErrorResponseException">Thrown when the operation encounters an error.</exception>
    //Task<OneOf<ResourcePreview, ErrorResponse>> HandleAsync(ResourcePreviewSpecification resourcePreviewSpecification, CancellationToken cancellationToken);
}
