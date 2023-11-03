// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Azure.Deployments.Extensibility.Core.V2.Contract
{
    public interface IExtensibilityProvider
    {
        Task<Results<Ok<ResourceReferenceResponseBody>, BadRequest<ErrorResponseBody>>> CreateResourceReferenceAsync(HttpContext httpContext, string providerVersion, ResourceRequestBody resourceRequestBody, CancellationToken cancellationToken);

        Task<Results<Ok<ResourceResponseBody>, BadRequest<ErrorResponseBody>>> PreviewResourceCreateOrUpdateAsync(HttpContext httpContext, string providerVersion, ResourceRequestBody resourceRequestBody, CancellationToken cancellationToken);

        Task<Results<Ok<ResourceResponseBody>, Created<ResourceResponseBody>, Accepted, BadRequest<ErrorResponseBody>, Conflict<ErrorResponseBody>>> CreateOrUpdateResourceAsync(HttpContext httpContext, string providerVersion, string referenceId, ResourceRequestBody resourceRequestBody, CancellationToken cancellationToken);

        Task<Results<Ok<ResourceResponseBody>, NotFound>> GetResourceByReferenceIdAsync(HttpContext httpContext, string providerVersion, string referenceId);

        Task<Results<NoContent, Accepted>> DeleteResourceByReferenceIdAsync(HttpContext httpContext, string providerVersion, string referenceId);

        Task<Results<Ok<ResourceOperationResponseBody>, NotFound>> GetResourceOperationByOperationIdAsync(HttpContext httpContext, string providerVersion, string operationId);
    }
}
