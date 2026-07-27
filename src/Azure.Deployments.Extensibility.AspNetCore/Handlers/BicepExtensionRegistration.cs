// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Behaviors;
using Azure.Deployments.Extensibility.AspNetCore.Handlers;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace Azure.Deployments.Extensibility.AspNetCore;

/// <summary>
/// Represents an immutable, version-independent Bicep extension handler registration.
/// </summary>
public sealed class BicepExtensionRegistration
{
    private readonly IReadOnlyDictionary<Type, ComponentRegistration> defaultHandlers;
    private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<Type, ComponentRegistration>> resourceTypeHandlers;
    private readonly IReadOnlyList<ComponentRegistration> behaviors;
    private readonly IReadOnlyDictionary<string, IReadOnlyList<ComponentRegistration>> resourceTypeBehaviors;

    internal BicepExtensionRegistration(
        IReadOnlyDictionary<Type, ComponentRegistration> defaultHandlers,
        IReadOnlyDictionary<string, Dictionary<Type, ComponentRegistration>> resourceTypeHandlers,
        IReadOnlyList<ComponentRegistration> behaviors,
        IReadOnlyDictionary<string, List<ComponentRegistration>> resourceTypeBehaviors)
    {
        this.defaultHandlers = new Dictionary<Type, ComponentRegistration>(defaultHandlers);
        this.resourceTypeHandlers = resourceTypeHandlers.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyDictionary<Type, ComponentRegistration>)new Dictionary<Type, ComponentRegistration>(pair.Value),
            StringComparer.OrdinalIgnoreCase);
        this.behaviors = behaviors.ToArray();
        this.resourceTypeBehaviors = resourceTypeBehaviors.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<ComponentRegistration>)pair.Value.ToArray(),
            StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Creates an immutable registration and adds its handler and behavior services to
    /// <paramref name="services"/>.
    /// </summary>
    public static BicepExtensionRegistration Create(
        IServiceCollection services,
        Action<IBicepExtensionBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new BicepExtensionBuilder(services);
        configure(builder);

        return builder.Build();
    }

    internal THandler? ResolveHandler<THandler>(string? resourceType, IServiceProvider serviceProvider)
        where THandler : class, IHandler
    {
        var handlerType = typeof(THandler);

        if (resourceType is not null &&
            this.resourceTypeHandlers.TryGetValue(resourceType, out var handlers) &&
            handlers.TryGetValue(handlerType, out var resourceTypeRegistration))
        {
            return (THandler)resourceTypeRegistration.Resolve(serviceProvider);
        }

        return this.defaultHandlers.TryGetValue(handlerType, out var defaultRegistration)
            ? (THandler)defaultRegistration.Resolve(serviceProvider)
            : null;
    }

    internal IReadOnlyList<IHandlerBehavior<TRequest, TResponse>> ResolveBehaviors<TRequest, TResponse>(
        string? resourceType,
        IServiceProvider serviceProvider)
    {
        var registrations = new List<ComponentRegistration>(this.behaviors);

        if (resourceType is not null && this.resourceTypeBehaviors.TryGetValue(resourceType, out var resourceBehaviors))
        {
            registrations.AddRange(resourceBehaviors);
        }

        return registrations
            .Select(registration => registration.Resolve(serviceProvider))
            .OfType<IHandlerBehavior<TRequest, TResponse>>()
            .ToArray();
    }
}
