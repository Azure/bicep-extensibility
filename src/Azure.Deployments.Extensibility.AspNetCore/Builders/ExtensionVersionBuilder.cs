// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Handlers;
using Azure.Deployments.Extensibility.AspNetCore.Pipeline;
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
    private readonly HandlerPipelineRegistry pipelineRegistry;
    private readonly SemVersionRange versionRange;

    internal ExtensionVersionBuilder(
        IServiceCollection services,
        HandlerRegistry registry,
        HandlerPipelineRegistry pipelineRegistry,
        SemVersionRange versionRange)
    {
        this.services = services;
        this.registry = registry;
        this.pipelineRegistry = pipelineRegistry;
        this.versionRange = versionRange;
    }

    /// <summary>
    /// Registers a pipeline behavior that will run for all handlers within this version range,
    /// but not for handlers in other version ranges.
    /// Version-scoped behaviors run after global behaviors but before handler-specific behaviors.
    /// </summary>
    public ExtensionVersionBuilder AddPipelineBehavior<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] TBehavior>()
        where TBehavior : class
    {
        this.services.TryAddScoped<TBehavior>();
        this.pipelineRegistry.AddVersionScoped(this.versionRange, typeof(TBehavior));

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
            this.pipelineRegistry,
            this.versionRange,
            resourceType);

        configure(builder);

        return this;
    }

    /// <summary>
    /// Registers a handler by auto-detecting which handler interfaces it implements.
    /// The handler is registered as a default (generic) handler for this version range.
    /// </summary>
    public ExtensionVersionBuilder AddHandler<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] THandler>(
        Action<HandlerPipelineBuilder>? configurePipeline = null)
        where THandler : class, IHandler
    {
        this.services.TryAddScoped<THandler>();
        this.registry.Register(typeof(THandler), this.versionRange, resourceType: null);

        if (configurePipeline is not null)
        {
            configurePipeline(new HandlerPipelineBuilder(this.services, this.pipelineRegistry, typeof(THandler)));
        }

        return this;
    }
}
