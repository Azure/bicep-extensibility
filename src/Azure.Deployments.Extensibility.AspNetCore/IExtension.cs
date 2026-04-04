// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using Microsoft.AspNetCore.Http;

namespace Azure.Deployments.Extensibility.AspNetCore
{
    /// <summary>
    /// Defines a V1-style extension that handles resource operations.
    /// </summary>
    [Obsolete("This interface is deprecated and will be removed in a future release.")]
    public interface IExtension
    {
        Task<IResult> PreviewResourceCreateOrUpdateAsync(HttpContext httpContext, ResourceSpecification resourceSpecification, CancellationToken cancellationToken);

        Task<IResult> CreateOrUpdateResourceAsync(HttpContext httpContext, ResourceSpecification resourceSpecification, CancellationToken cancellationToken);

        Task<IResult> GetResourceAsync(HttpContext httpContext, ResourceReference resourceReference, CancellationToken cancellationToken);

        Task<IResult> DeleteResourceAsync(HttpContext httpContext, ResourceReference resourceReference, CancellationToken cancellationToken);
    }
}
