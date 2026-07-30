// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Hosting;

namespace Azure.Deployments.Extensibility.Hosting.Managed.Validation;

internal sealed class ManagedExtensionStartupValidator : IHostedService
{
    private readonly ManagedExtensionState state;

    public ManagedExtensionStartupValidator(ManagedExtensionState state)
    {
        this.state = state;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!this.state.ApplicationConfigured)
        {
            throw new InvalidOperationException(
                "Call UseBicepExtension before starting the application.");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
