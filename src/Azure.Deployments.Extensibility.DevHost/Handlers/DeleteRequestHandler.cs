// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core;

namespace Azure.Deployments.Extensibility.DevHost.Handlers
{
    public class DeleteRequestHandler : ExtensibilityRequestHandler
    {
        public DeleteRequestHandler()
            : base("/delete")
        {
        }

        protected override ExtensibilityOperation SelectProviderOperation(IExtensibilityProvider provider) => provider.DeleteAsync;
    }
}
