// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Models;
using Microsoft.AspNetCore.Http;

namespace Azure.Deployments.Extensibility.AspNetCore
{
    public interface IDeploymentExtension
    {
        Task<IResult> PreviewResourceCreateOrUpdateAsync(HttpContext httpContext, ResourceSpecification resourceSpecification, CancellationToken cancellationToken);

        Task<IResult> CreateOrUpdateResourceAsync(HttpContext httpContext, ResourceSpecification resourceSpecification, CancellationToken cancellationToken);

        Task<IResult> GetResourceAsync(HttpContext httpContext, ResourceReference resourceReference, CancellationToken cancellationToken);

        Task<IResult> DeleteResourceAsync(HttpContext httpContext, ResourceReference resourceReference, CancellationToken cancellationToken);
    }
}
