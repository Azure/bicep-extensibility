// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Behaviors;
using Azure.Deployments.Extensibility.AspNetCore.Handlers;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Semver;
using System.Diagnostics.CodeAnalysis;

namespace Azure.Deployments.Extensibility.AspNetCore;

/// <summary>
/// Configures handler registrations for a specific extension version range.
/// Handlers registered directly on this builder are default (generic) handlers.
/// Use <see cref="ForResourceType"/> to register handlers scoped to a named resource type.
/// </summary>
public sealed class ExtensionVersionBuilder
{
    private readonly IServiceCollection services;
    private readonly HandlerRegistry registry;
    private readonly HandlerBehaviorRegistry decoratorRegistry;
    private readonly SemVersionRange versionRange;

    internal ExtensionVersionBuilder(
        IServiceCollection services,
        HandlerRegistry registry,
        HandlerBehaviorRegistry decoratorRegistry,
        SemVersionRange versionRange)
    {
        this.services = services;
        this.registry = registry;
        this.decoratorRegistry = decoratorRegistry;
        this.versionRange = versionRange;
    }

    /// <summary>
    /// Registers a handler behavior that will run for all handlers within this version range,
    /// but not for handlers in other version ranges.
    /// The behavior is resolved from the DI container as a scoped service per request.
    /// Version-scoped behaviors run after global behaviors but before resource-type-scoped behaviors.
    /// </summary>
    public ExtensionVersionBuilder AddHandlerBehavior<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] TBehavior>()
        where TBehavior : class
    {
        this.services.TryAddScoped<TBehavior>();
        this.decoratorRegistry.AddVersionScoped(this.versionRange, sp => sp.GetRequiredService<TBehavior>());

        return this;
    }

    /// <summary>
    /// Registers a handler behavior using a factory that will run for all handlers within this version range,
    /// but not for handlers in other version ranges.
    /// The factory is invoked on every request; the returned instance is not managed by the DI container.
    /// Use this overload when the behavior requires constructor arguments not available in DI.
    /// Version-scoped behaviors run after global behaviors but before resource-type-scoped behaviors.
    /// </summary>
    public ExtensionVersionBuilder AddHandlerBehavior<TBehavior>(Func<IServiceProvider, TBehavior> factory)
        where TBehavior : class
    {
        this.decoratorRegistry.AddVersionScoped(this.versionRange, sp => factory(sp));

        return this;
    }

    /// <summary>
    /// Groups handler registrations for a specific resource type.
    /// Handlers registered inside the callback are only dispatched when the incoming
    /// request targets the specified <paramref name="resourceType"/>.
    /// </summary>
    public ExtensionVersionBuilder ForResourceType(string resourceType, Action<ResourceTypeBuilder> configure)
    {
        var builder = new ResourceTypeBuilder(
            this.services,
            this.registry,
            this.decoratorRegistry,
            this.versionRange,
            resourceType);

        configure(builder);

        return this;
    }

    /// <summary>
    /// Registers a handler by auto-detecting which handler interfaces it implements.
    /// The handler is registered as a default (generic) handler for this version range.
    /// </summary>
    public ExtensionVersionBuilder AddHandler<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] THandler>()
        where THandler : class, IHandler
    {
        this.services.TryAddScoped<THandler>();
        this.registry.Register(typeof(THandler), this.versionRange, resourceType: null);

        return this;
    }
}
