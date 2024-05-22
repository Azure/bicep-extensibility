// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Models;
using Microsoft.AspNetCore.Http;

namespace Azure.Deployments.Extensibility.AspNetCore
{
    public abstract class DeploymentExtensionDispatcher
    {
        protected virtual Task<IResult> PreviewResourceCreateOrUpdateAsync(string extensionVersion, HttpContext httpContext, ResourceSpecification resourceSpecification, CancellationToken cancellationToken) =>
            this.ResolveExtension(extensionVersion).PreviewResourceCreateOrUpdateAsync(httpContext, resourceSpecification, cancellationToken);

        protected virtual Task<IResult> CreateOrUpdateResourceAsync(string extensionVersion, HttpContext httpContext, ResourceSpecification resourceSpecification, CancellationToken cancellationToken) =>
            this.ResolveExtension(extensionVersion).CreateOrUpdateResourceAsync(httpContext, resourceSpecification, cancellationToken);

        protected virtual Task<IResult> GetResourceAsync(string extensionVersion, HttpContext httpContext, ResourceReference resourceReference, CancellationToken cancellationToken) =>
            this.ResolveExtension(extensionVersion).GetResourceAsync(httpContext, resourceReference, cancellationToken);

        protected Task<IResult> DeleteResourceAsync(string extensionVersion, HttpContext httpContext, ResourceReference resourceReference, CancellationToken cancellationToken) =>
            this.ResolveExtension(extensionVersion).DeleteResourceAsync(httpContext, resourceReference, cancellationToken);

        protected abstract IDeploymentExtension ResolveExtension(string extensionVersion);
    }
}
