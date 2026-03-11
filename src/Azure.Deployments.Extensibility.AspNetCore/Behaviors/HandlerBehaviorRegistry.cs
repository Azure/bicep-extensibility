// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Semver;

namespace Azure.Deployments.Extensibility.AspNetCore.Behaviors;

/// <summary>
/// Tracks behavior registrations at three scopes: global, version-scoped, and resource-type-scoped.
/// Populated at startup; read-only at dispatch time.
/// </summary>
public sealed class HandlerBehaviorRegistry
{
    private readonly Dictionary<(SemVersionRange, string), List<Func<IServiceProvider, object>>> behaviorsByResourceType = [];
    private readonly Dictionary<SemVersionRange, List<Func<IServiceProvider, object>>> behaviorsByVersionRange = [];
    private readonly List<Func<IServiceProvider, object>> globalBehaviorFactories = [];

    /// <summary>
    /// Records that the behavior produced by <paramref name="factory"/> should run
    /// for all handlers registered under <paramref name="versionRange"/> and <paramref name="resourceType"/>.
    /// Resource-type-scoped behaviors run after version-scoped behaviors but before the handler.
    /// </summary>
    public void AddResourceTypeScoped(SemVersionRange versionRange, string resourceType, Func<IServiceProvider, object> factory)
    {
        var key = (versionRange, resourceType);

        if (!this.behaviorsByResourceType.TryGetValue(key, out var factories))
        {
            factories = [];
            this.behaviorsByResourceType[key] = factories;
        }

        factories.Add(factory);
    }

    /// <summary>
    /// Records that the behavior produced by <paramref name="factory"/> should run
    /// for all handlers registered under <paramref name="versionRange"/>.
    /// Version-scoped behaviors run after global behaviors but before resource-type-scoped behaviors.
    /// </summary>
    public void AddVersionScoped(SemVersionRange versionRange, Func<IServiceProvider, object> factory)
    {
        if (!this.behaviorsByVersionRange.TryGetValue(versionRange, out var factories))
        {
            factories = [];
            this.behaviorsByVersionRange[versionRange] = factories;
        }

        factories.Add(factory);
    }

    /// <summary>
    /// Records that the behavior produced by <paramref name="factory"/> should run
    /// for all handlers. Global behaviors are outermost in the chain.
    /// </summary>
    public void AddGlobal(Func<IServiceProvider, object> factory)
    {
        this.globalBehaviorFactories.Add(factory);
    }

    /// <summary>
    /// Resolves behaviors for the given version and resource type, filtered to those
    /// that implement <see cref="IHandlerBehavior{TRequest, TResponse}"/>
    /// matching the requested request/response types.
    /// Execution order (outermost first): global → version-scoped → resource-type-scoped.
    /// </summary>
    public IReadOnlyList<IHandlerBehavior<TRequest, TResponse>> Resolve<TRequest, TResponse>(
        SemVersionRange? versionRange,
        string? resourceType,
        IServiceProvider serviceProvider)
    {
        var allFactories = new List<Func<IServiceProvider, object>>(this.globalBehaviorFactories);

        if (versionRange is not null && this.behaviorsByVersionRange.TryGetValue(versionRange, out var versionScoped))
        {
            allFactories.AddRange(versionScoped);
        }

        if (versionRange is not null && resourceType is not null &&
            this.behaviorsByResourceType.TryGetValue((versionRange, resourceType), out var resourceTypeScoped))
        {
            allFactories.AddRange(resourceTypeScoped);
        }

        if (allFactories.Count == 0)
        {
            return [];
        }

        return allFactories
            .Select(factory => factory(serviceProvider))
            .OfType<IHandlerBehavior<TRequest, TResponse>>()
            .ToArray();
    }
}
