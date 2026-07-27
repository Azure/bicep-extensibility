// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;

namespace Azure.Deployments.Extensibility.AspNetCore.Handlers;

internal sealed class ComponentRegistration
{
    private readonly Type serviceType;
    private readonly object? serviceKey;

    internal ComponentRegistration(Type serviceType, object? serviceKey = null)
    {
        this.serviceType = serviceType;
        this.serviceKey = serviceKey;
    }

    internal object Resolve(IServiceProvider serviceProvider) => this.serviceKey is null
        ? serviceProvider.GetRequiredService(this.serviceType)
        : serviceProvider.GetRequiredKeyedService(this.serviceType, this.serviceKey);
}
