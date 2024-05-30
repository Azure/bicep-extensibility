// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Deployments.Extensibility.AspNetCore
{
    public interface IExtensionDispatcher
    {
        public IExtension DispatchExtension(string extensionVersion);
    }
}
