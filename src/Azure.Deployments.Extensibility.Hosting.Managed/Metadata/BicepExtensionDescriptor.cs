// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Deployments.Extensibility.Hosting.Managed.Metadata;

internal sealed class BicepExtensionDescriptor
{
    internal BicepExtensionDescriptor(string name, string version)
    {
        this.Name = name;
        this.Version = version;
    }

    internal string Name { get; }

    internal string Version { get; }
}