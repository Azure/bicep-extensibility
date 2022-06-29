// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core;

namespace Azure.Deployments.Extensibility.DevHost.Handlers
{
    public class GetRequestHandler : ExtensibilityRequestHandler
    {
        public GetRequestHandler()
            : base("/get")
        {
        }

        protected override ExtensibilityOperation SelectProviderOperation(IExtensibilityProvider provider) => provider.GetAsync;
    }
}
