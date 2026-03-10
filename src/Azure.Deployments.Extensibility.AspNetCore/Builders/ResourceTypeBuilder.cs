// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Decorators;
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
    private readonly HandlerDecoratorRegistry decoratorRegistry;
    private readonly SemVersionRange versionRange;
    private readonly string resourceType;

    internal ResourceTypeBuilder(
        IServiceCollection services,
        HandlerRegistry registry,
        HandlerDecoratorRegistry decoratorRegistry,
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
    public ResourceTypeBuilder AddHandler<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] THandler>(
        Action<HandlerDecoratorBuilder>? configureDecorators = null)
        where THandler : class, IHandler
    {
        this.services.TryAddScoped<THandler>();
        this.registry.Register(typeof(THandler), this.versionRange, this.resourceType);

        if (configureDecorators is not null)
        {
            configureDecorators(new HandlerDecoratorBuilder(this.services, this.decoratorRegistry, typeof(THandler)));
        }

        return this;
    }
}
