// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Deployments.Extensibility.AspNetCore
{
    /// <summary>
    /// Dispatches requests to the appropriate <see cref="IExtension"/> based on extension version.
    /// </summary>
    [Obsolete("This interface is deprecated and will be removed in a future release.")]
    public interface IExtensionDispatcher
    {
        public IExtension DispatchExtension(string extensionVersion);
    }
}
