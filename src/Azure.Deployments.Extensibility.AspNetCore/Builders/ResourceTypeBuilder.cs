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
/// Configures handler registrations scoped to a specific resource type
/// within an extension version range.
/// </summary>
public sealed class ResourceTypeBuilder
{
    private readonly IServiceCollection services;
    private readonly HandlerRegistry registry;
    private readonly HandlerBehaviorRegistry decoratorRegistry;
    private readonly SemVersionRange versionRange;
    private readonly string resourceType;

    internal ResourceTypeBuilder(
        IServiceCollection services,
        HandlerRegistry registry,
        HandlerBehaviorRegistry decoratorRegistry,
        SemVersionRange versionRange,
        string resourceType)
    {
        this.services = services;
        this.registry = registry;
        this.decoratorRegistry = decoratorRegistry;
        this.versionRange = versionRange;
        this.resourceType = resourceType;
    }

    /// <summary>
    /// Registers a handler by auto-detecting which handler interfaces it implements.
    /// </summary>
    public ResourceTypeBuilder AddHandler<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] THandler>()
        where THandler : class, IHandler
    {
        this.services.TryAddScoped<THandler>();
        this.registry.Register(typeof(THandler), this.versionRange, this.resourceType);

        return this;
    }

    /// <summary>
    /// Registers a handler behavior that will run for all handlers within this resource type,
    /// but not for handlers targeting other resource types.
    /// The behavior is resolved from the DI container as a scoped service per request.
    /// Resource-type-scoped behaviors run after version-scoped behaviors but before the handler.
    /// </summary>
    public ResourceTypeBuilder AddHandlerBehavior<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] TBehavior>()
        where TBehavior : class
    {
        this.services.TryAddScoped<TBehavior>();
        this.decoratorRegistry.AddResourceTypeScoped(this.versionRange, this.resourceType, sp => sp.GetRequiredService<TBehavior>());

        return this;
    }

    /// <summary>
    /// Registers a handler behavior using a factory that will run for all handlers within this resource type,
    /// but not for handlers targeting other resource types.
    /// The factory is invoked on every request; the returned instance is not managed by the DI container.
    /// Use this overload when the behavior requires constructor arguments not available in DI.
    /// Resource-type-scoped behaviors run after version-scoped behaviors but before the handler.
    /// </summary>
    public ResourceTypeBuilder AddHandlerBehavior<TBehavior>(Func<IServiceProvider, TBehavior> factory)
        where TBehavior : class
    {
        this.decoratorRegistry.AddResourceTypeScoped(this.versionRange, this.resourceType, sp => factory(sp));

        return this;
    }
}
