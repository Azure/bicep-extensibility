// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Deployments.Extensibility.Hosting.Managed.Identity;

internal sealed class BicepExtensionIdentity
{
    internal BicepExtensionIdentity(string name, string version)
    {
        this.Name = name;
        this.Version = version;
    }

    internal string Name { get; }

    internal string Version { get; }
}
