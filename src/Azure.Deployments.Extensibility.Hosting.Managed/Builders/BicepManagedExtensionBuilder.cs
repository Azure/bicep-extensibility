// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace Azure.Deployments.Extensibility.Hosting.Managed;

internal sealed class BicepManagedExtensionBuilder : IBicepManagedExtensionBuilder
{
    private readonly IServiceCollection services;
    private readonly IBicepExtensionBuilder extensionBuilder;

    internal BicepManagedExtensionBuilder(
        IServiceCollection services,
        IBicepExtensionBuilder extensionBuilder)
    {
        this.services = services;
        this.extensionBuilder = extensionBuilder;
    }

    public IBicepManagedExtensionBuilder AddGlobalHandlerBehavior<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] TBehavior>()
        where TBehavior : class
    {
        this.services.AddBicepExtensionGlobalHandlerBehavior<TBehavior>();
        return this;
    }

    public IBicepManagedExtensionBuilder AddGlobalHandlerBehavior<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] TBehavior>(
        Func<IServiceProvider, TBehavior> factory)
        where TBehavior : class
    {
        this.services.AddBicepExtensionGlobalHandlerBehavior(factory);
        return this;
    }

    public IBicepManagedExtensionBuilder AddHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] THandler>()
        where THandler : class, IHandler
    {
        this.extensionBuilder.AddHandler<THandler>();
        return this;
    }

    public IBicepManagedExtensionBuilder AddHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] THandler>(
        Func<IServiceProvider, THandler> factory)
        where THandler : class, IHandler
    {
        this.extensionBuilder.AddHandler(factory);
        return this;
    }

    public IBicepManagedExtensionBuilder AddHandlerBehavior<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] TBehavior>()
        where TBehavior : class
    {
        this.extensionBuilder.AddHandlerBehavior<TBehavior>();
        return this;
    }

    public IBicepManagedExtensionBuilder AddHandlerBehavior<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] TBehavior>(
        Func<IServiceProvider, TBehavior> factory)
        where TBehavior : class
    {
        this.extensionBuilder.AddHandlerBehavior(factory);
        return this;
    }

    public IBicepManagedExtensionBuilder ForResourceType(
        string resourceType,
        Action<IBicepResourceTypeBuilder> configure)
    {
        this.extensionBuilder.ForResourceType(resourceType, configure);
        return this;
    }
}
