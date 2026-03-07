// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Deployments.Extensibility.AspNetCore
{
    [Obsolete("This interface is deprecated and will be removed in a future release.")]
    public interface IExtensionDispatcher
    {
        public IExtension DispatchExtension(string extensionVersion);
    }
}
