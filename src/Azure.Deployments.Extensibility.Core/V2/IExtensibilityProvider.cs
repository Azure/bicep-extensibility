// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Models;
using Microsoft.AspNetCore.Http;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Core.V2
{
    public interface IExtensibilityProvider
    {
        Task<IResult> CreateResourceReferenceAsync(HttpContext httpContext, string providerVersion, ResourceRequestBody resourceRequestBody, CancellationToken cancellationToken);

        Task<IResult> PreviewResourceCreateOrUpdateAsync(HttpContext httpContext, string providerVersion, ResourceRequestBody resourceRequestBody, CancellationToken cancellationToken);

        Task<IResult> CreateOrUpdateResourceAsync(HttpContext httpContext, string providerVersion, string referenceId, ResourceRequestBody resourceRequestBody, CancellationToken cancellationToken);

        Task<IResult> GetResourceByReferenceIdAsync(HttpContext httpContext, string providerVersion, string referenceId, CancellationToken cancellationToken);

        Task<IResult> GetResourceByReferenceIdWithConfigAsync(HttpContext httpContext, string providerVersion, string referenceId, JsonObject config, CancellationToken cancellationToken);

        Task<IResult> DeleteResourceByReferenceIdAsync(HttpContext httpContext, string providerVersion, string referenceId, CancellationToken cancellationToken);

        Task<IResult> DeleteResourceByReferenceIdWithConfigAsync(HttpContext httpContext, string providerVersion, string referenceId, JsonObject config, CancellationToken cancellationToken);

        Task<IResult> GetResourceOperationByOperationIdAsync(HttpContext httpContext, string providerVersion, string operationId, CancellationToken cancellationToken);
    }
}
