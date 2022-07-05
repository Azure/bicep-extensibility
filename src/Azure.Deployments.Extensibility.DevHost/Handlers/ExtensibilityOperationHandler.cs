// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core;
using Json.Pointer;

namespace Azure.Deployments.Extensibility.DevHost.Handlers
{
    public abstract class ExtensibilityRequestHandler
    {
        private readonly string routePattern;

        public ExtensibilityRequestHandler(string routePattern)
        {
            this.routePattern = routePattern;
        }

        public void RegisterRoute(IEndpointRouteBuilder builder) => builder.MapPost(this.routePattern, this.HandleAsync);

        protected virtual async Task<ExtensibilityOperationResponse> HandleAsync(ExtensibilityOperationRequest request, IExtensibilityProviderRegistry registry, CancellationToken cancellationToken)
        {
            var providerName = request.Import.Provider;
            var provider = registry.TryGetExtensibilityProvider(providerName);

            if (provider is null)
            {
                return new ExtensibilityOperationErrorResponse(
                    new ExtensibilityError(
                        "UnknownExtensibilityProvider",
                        JsonPointer.Parse($"/imports/provider"),
                        @$"Unknown extensibility provider: ""{providerName}""."));
            }

            var operation = this.SelectProviderOperation(provider);
            var response = await operation.Invoke(request, cancellationToken);

            return response;
        }

        protected abstract ExtensibilityOperation SelectProviderOperation(IExtensibilityProvider provider);
    }
}
