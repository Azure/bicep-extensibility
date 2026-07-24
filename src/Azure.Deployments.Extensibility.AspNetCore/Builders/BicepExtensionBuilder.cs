// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Behaviors;
using Azure.Deployments.Extensibility.AspNetCore.Handlers;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Diagnostics.CodeAnalysis;

namespace Azure.Deployments.Extensibility.AspNetCore;

internal sealed class BicepExtensionBuilder : IBicepExtensionBuilder
{
    private static readonly Type[] DetectableHandlerInterfaces =
    [
        typeof(IResourcePreviewHandler),
        typeof(IResourceCreateOrUpdateHandler),
        typeof(IResourceGetHandler),
        typeof(IResourceDeleteHandler),
        typeof(ILongRunningOperationGetHandler),
    ];

    private readonly IServiceCollection services;
    private readonly Dictionary<Type, ComponentRegistration> defaultHandlers = [];
    private readonly Dictionary<string, Dictionary<Type, ComponentRegistration>> resourceTypeHandlers = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<ComponentRegistration> behaviors = [];
    private readonly Dictionary<string, List<ComponentRegistration>> resourceTypeBehaviors = new(StringComparer.OrdinalIgnoreCase);

    internal BicepExtensionBuilder(IServiceCollection services)
    {
        this.services = services;
    }

    public IBicepExtensionBuilder AddHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] THandler>()
        where THandler : class, IHandler
    {
        this.AddHandler<THandler>(resourceType: null, factory: null);
        return this;
    }

    public IBicepExtensionBuilder AddHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] THandler>(
        Func<IServiceProvider, THandler> factory)
        where THandler : class, IHandler
    {
        ArgumentNullException.ThrowIfNull(factory);

        this.AddHandler(resourceType: null, factory);
        return this;
    }

    public IBicepExtensionBuilder AddHandlerBehavior<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] TBehavior>()
        where TBehavior : class
    {
        this.AddBehavior<TBehavior>(resourceType: null, factory: null);
        return this;
    }

    public IBicepExtensionBuilder AddHandlerBehavior<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] TBehavior>(
        Func<IServiceProvider, TBehavior> factory)
        where TBehavior : class
    {
        ArgumentNullException.ThrowIfNull(factory);

        this.AddBehavior(resourceType: null, factory);
        return this;
    }

    public IBicepExtensionBuilder ForResourceType(string resourceType, Action<IBicepResourceTypeBuilder> configure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceType);
        ArgumentNullException.ThrowIfNull(configure);

        configure(new BicepResourceTypeBuilder(this, resourceType));
        return this;
    }

    internal BicepExtensionRegistration Build()
    {
        if (this.defaultHandlers.Count == 0 && this.resourceTypeHandlers.Values.All(handlers => handlers.Count == 0))
        {
            throw new InvalidOperationException("The Bicep extension registration must contain at least one handler.");
        }

        return new BicepExtensionRegistration(
            this.defaultHandlers,
            this.resourceTypeHandlers,
            this.behaviors,
            this.resourceTypeBehaviors);
    }

    internal void AddResourceTypeHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] THandler>(
        string resourceType,
        Func<IServiceProvider, THandler>? factory)
        where THandler : class, IHandler => this.AddHandler(resourceType, factory);

    internal void AddResourceTypeBehavior<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] TBehavior>(
        string resourceType,
        Func<IServiceProvider, TBehavior>? factory)
        where TBehavior : class => this.AddBehavior(resourceType, factory);

    private void AddHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] THandler>(
        string? resourceType,
        Func<IServiceProvider, THandler>? factory)
        where THandler : class, IHandler
    {
        var handlerInterfaces = DetectableHandlerInterfaces
            .Where(handlerInterface => handlerInterface.IsAssignableFrom(typeof(THandler)))
            .ToArray();

        if (handlerInterfaces.Length == 0)
        {
            throw new InvalidOperationException(
                $"Handler type '{typeof(THandler)}' does not implement a supported Bicep extension handler interface.");
        }

        if (resourceType is not null && handlerInterfaces.Contains(typeof(ILongRunningOperationGetHandler)))
        {
            throw new InvalidOperationException(
                $"Long-running operation handlers cannot be scoped to resource type '{resourceType}'.");
        }

        var handlers = this.GetHandlers(resourceType);
        var duplicateInterface = handlerInterfaces.FirstOrDefault(handlers.ContainsKey);

        if (duplicateInterface is not null)
        {
            var scope = resourceType is null ? "the default scope" : $"resource type '{resourceType}'";
            throw new InvalidOperationException(
                $"A handler for operation '{duplicateInterface}' is already registered in {scope}.");
        }

        var registration = factory is null
            ? this.RegisterComponent<THandler>()
            : this.RegisterComponent(factory);

        foreach (var handlerInterface in handlerInterfaces)
        {
            handlers.Add(handlerInterface, registration);
        }
    }

    private void AddBehavior<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] TBehavior>(
        string? resourceType,
        Func<IServiceProvider, TBehavior>? factory)
        where TBehavior : class
    {
        if (!HandlerContractTypes.IsSupportedBehavior(typeof(TBehavior)))
        {
            throw new InvalidOperationException(
                $"Behavior type '{typeof(TBehavior)}' does not implement a supported handler behavior interface.");
        }

        var registration = factory is null
            ? this.RegisterComponent<TBehavior>()
            : this.RegisterComponent(factory);

        this.GetBehaviors(resourceType).Add(registration);
    }

    private Dictionary<Type, ComponentRegistration> GetHandlers(string? resourceType)
    {
        if (resourceType is null)
        {
            return this.defaultHandlers;
        }

        if (!this.resourceTypeHandlers.TryGetValue(resourceType, out var handlers))
        {
            handlers = [];
            this.resourceTypeHandlers.Add(resourceType, handlers);
        }

        return handlers;
    }

    private List<ComponentRegistration> GetBehaviors(string? resourceType)
    {
        if (resourceType is null)
        {
            return this.behaviors;
        }

        if (!this.resourceTypeBehaviors.TryGetValue(resourceType, out var registrations))
        {
            registrations = [];
            this.resourceTypeBehaviors.Add(resourceType, registrations);
        }

        return registrations;
    }

    private ComponentRegistration RegisterComponent<TComponent>()
        where TComponent : class
    {
        this.services.TryAddScoped<TComponent>();
        return new ComponentRegistration(typeof(TComponent));
    }

    private ComponentRegistration RegisterComponent<TComponent>(Func<IServiceProvider, TComponent> factory)
        where TComponent : class
    {
        var serviceKey = new object();
        this.services.AddKeyedScoped<TComponent>(serviceKey, (serviceProvider, _) => factory(serviceProvider));

        return new ComponentRegistration(typeof(TComponent), serviceKey);
    }
}

internal sealed class BicepResourceTypeBuilder : IBicepResourceTypeBuilder
{
    private readonly BicepExtensionBuilder extensionBuilder;
    private readonly string resourceType;

    internal BicepResourceTypeBuilder(BicepExtensionBuilder extensionBuilder, string resourceType)
    {
        this.extensionBuilder = extensionBuilder;
        this.resourceType = resourceType;
    }

    public IBicepResourceTypeBuilder AddHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] THandler>()
        where THandler : class, IHandler
    {
        this.extensionBuilder.AddResourceTypeHandler<THandler>(this.resourceType, factory: null);
        return this;
    }

    public IBicepResourceTypeBuilder AddHandler<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] THandler>(
        Func<IServiceProvider, THandler> factory)
        where THandler : class, IHandler
    {
        ArgumentNullException.ThrowIfNull(factory);

        this.extensionBuilder.AddResourceTypeHandler(this.resourceType, factory);
        return this;
    }

    public IBicepResourceTypeBuilder AddHandlerBehavior<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] TBehavior>()
        where TBehavior : class
    {
        this.extensionBuilder.AddResourceTypeBehavior<TBehavior>(this.resourceType, factory: null);
        return this;
    }

    public IBicepResourceTypeBuilder AddHandlerBehavior<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] TBehavior>(
        Func<IServiceProvider, TBehavior> factory)
        where TBehavior : class
    {
        ArgumentNullException.ThrowIfNull(factory);

        this.extensionBuilder.AddResourceTypeBehavior(this.resourceType, factory);
        return this;
    }
}
