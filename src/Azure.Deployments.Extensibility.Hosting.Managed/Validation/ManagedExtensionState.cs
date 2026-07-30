// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Deployments.Extensibility.Hosting.Managed.Validation;

internal sealed class ManagedExtensionState
{
    private int applicationConfigured;

    internal ManagedExtensionState(string extensionVersion)
    {
        this.ExtensionVersion = extensionVersion;
    }

    internal string ExtensionVersion { get; }

    internal bool ApplicationConfigured =>
        Volatile.Read(ref this.applicationConfigured) == 1;

    internal void MarkApplicationConfigured()
    {
        if (Interlocked.Exchange(ref this.applicationConfigured, 1) == 1)
        {
            throw new InvalidOperationException(
                "UseBicepExtension can only be called once.");
        }
    }
}
