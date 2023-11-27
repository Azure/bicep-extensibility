// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Core.V2
{
    /// <summary>
    /// Defines API contract for extensibility providers.
    /// </summary>
    /// <remarks>Refer to <see href="https://redocly.github.io/redoc/?url=https://raw.githubusercontent.com/Azure/bicep-extensibility/main/docs/vnext/openapi.yaml"/> for more detials.</remarks>
    public interface IExtensibilityProvider
    {
        /// <summary>
        /// Creates a reference to a resource.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext"/> instance for the current request.</param>
        /// <param name="providerVersion">The provider version specified in the Bicep provider import statement.</param>
        /// <param name="resourceRequestBody">The request body containing the resource definition.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns><see cref="Ok"/> with <see cref="ResourceReferenceResponseBody"/>, or <see cref="BadRequest"/> with <see cref="ErrorResponseBody"/>.</returns>
        Task<IResult> CreateResourceReferenceAsync(HttpContext httpContext, string providerVersion, ResourceRequestBody resourceRequestBody, CancellationToken cancellationToken);

        /// <summary>
        /// Previews a resource create or update operation.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext"/> instance for the current request.</param>
        /// <param name="providerVersion">The provider version specified in the Bicep provider import statement.</param>
        /// <param name="resourceRequestBody">The request body containing the resource definition.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns><see cref="Ok"/> with <see cref="ResourceReferenceResponseBody"/>, or <see cref="BadRequest"/> with <see cref="ErrorResponseBody"/>.</returns>
        Task<IResult> PreviewResourceCreateOrUpdateAsync(HttpContext httpContext, string providerVersion, ResourceRequestBody resourceRequestBody, CancellationToken cancellationToken);

        /// <summary>
        /// Creates or updates a resource.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext"/> instance for the current request.</param>
        /// <param name="providerVersion">The provider version specified in the Bicep provider import statement.</param>
        /// <param name="referenceId">The reference ID of the resource to create or update.</param>
        /// <param name="resourceRequestBody">The request body containing the resource definition.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns><see cref="Ok"/>/<see cref="Created"/> with <see cref="ResourceReferenceResponseBody"/>, or <see cref="Accepted"/> with a Location header, or <see cref="BadRequest"/> with <see cref="ErrorResponseBody"/>.</returns>
        Task<IResult> CreateOrUpdateResourceAsync(HttpContext httpContext, string providerVersion, string referenceId, ResourceRequestBody resourceRequestBody, CancellationToken cancellationToken);

        /// <summary>
        /// Gets a resource by a reference ID.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext"/> instance for the current request.</param>
        /// <param name="providerVersion">The provider version specified in the Bicep provider import statement.</param>
        /// <param name="referenceId">The reference ID of the resource to get.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns><see cref="Ok"/> with <see cref="ResourceReferenceResponseBody"/>, or <see cref="NotFound"/></returns>
        Task<IResult> GetResourceByReferenceIdAsync(HttpContext httpContext, string providerVersion, string referenceId, CancellationToken cancellationToken);

        /// <summary>
        /// Gets a resource by a reference ID with a POST method and a payload containing the provider config.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext"/> instance for the current request.</param>
        /// <param name="providerVersion">The provider version specified in the Bicep provider import statement.</param>
        /// <param name="referenceId">The reference ID of the resource to get.</param>
        /// <param name="config">The provider config required to perform the GET operation.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns><see cref="Ok"/> with <see cref="ResourceReferenceResponseBody"/>, or <see cref="NotFound"/></returns>
        Task<IResult> GetResourceByReferenceIdWithConfigAsync(HttpContext httpContext, string providerVersion, string referenceId, JsonObject config, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes a resource by a reference ID.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext"/> instance for the current request.</param>
        /// <param name="providerVersion">The provider version specified in the Bicep provider import statement.</param>
        /// <param name="referenceId">The reference ID of the resource to get.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns><see cref="NoContent"/>, or <see cref="Accepted"/> with a Location header.</returns>
        Task<IResult> DeleteResourceByReferenceIdAsync(HttpContext httpContext, string providerVersion, string referenceId, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes a resource by a reference ID with a POST method and a payload containing the provider config.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext"/> instance for the current request.</param>
        /// <param name="providerVersion">The provider version specified in the Bicep provider import statement.</param>
        /// <param name="referenceId">The reference ID of the resource to get.</param>
        /// <param name="config">The provider config required to perform the DELETE operation.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns><see cref="NoContent"/>, or <see cref="Accepted"/> with a Location header.</returns>
        Task<IResult> DeleteResourceByReferenceIdWithConfigAsync(HttpContext httpContext, string providerVersion, string referenceId, JsonObject config, CancellationToken cancellationToken);

        /// <summary>
        /// Gets a resource stepwise long-running operation by an operation ID.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext"/> instance for the current request.</param>
        /// <param name="providerVersion">The provider version specified in the Bicep provider import statement.</param>
        /// <param name="operationId">The long-running operation ID.</param>
        /// <param name="cancellationToken">The cancellation token for the request.</param>
        /// <returns><see cref="Ok"/> with <see cref="ResourceOperationResponseBody"/>.</returns>
        Task<IResult> GetResourceOperationByOperationIdAsync(HttpContext httpContext, string providerVersion, string operationId, CancellationToken cancellationToken);
    }
}
