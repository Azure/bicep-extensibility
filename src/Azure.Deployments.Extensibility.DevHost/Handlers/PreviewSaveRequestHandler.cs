// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core;

namespace Azure.Deployments.Extensibility.DevHost.Handlers
{
    public class PreviewSaveRequestHandler : ExtensibilityRequestHandler
    {
        public PreviewSaveRequestHandler()
            : base("/api/previewSave")
        {
        }

        protected override ExtensibilityOperation SelectProviderOperation(IExtensibilityProvider provider) => provider.PreviewSaveAsync;
    }
}
