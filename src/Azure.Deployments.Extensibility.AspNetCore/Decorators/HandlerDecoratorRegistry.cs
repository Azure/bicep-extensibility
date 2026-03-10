// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Semver;

namespace Azure.Deployments.Extensibility.AspNetCore.Decorators;

/// <summary>
/// Maps handler concrete types to their registered decorator types.
/// Populated at startup; read-only at dispatch time.
/// </summary>
public sealed class HandlerDecoratorRegistry
{
    private readonly Dictionary<Type, List<Type>> decoratorsByHandlerType = [];
    private readonly Dictionary<SemVersionRange, List<Type>> decoratorsByVersionRange = [];
    private readonly List<Type> globalDecoratorTypes = [];

    /// <summary>
    /// Records that <paramref name="decoratorType"/> should run
    /// for the handler identified by <paramref name="handlerType"/>.
    /// </summary>
    public void AddHandlerScoped(Type handlerType, Type decoratorType)
    {
        if (!this.decoratorsByHandlerType.TryGetValue(handlerType, out var types))
        {
            types = [];
            this.decoratorsByHandlerType[handlerType] = types;
        }

        types.Add(decoratorType);
    }

    /// <summary>
    /// Records that <paramref name="decoratorType"/> should run
    /// for all handlers registered under <paramref name="versionRange"/>.
    /// Version-scoped decorators run after global decorators but before handler-specific decorators.
    /// </summary>
    public void AddVersionScoped(SemVersionRange versionRange, Type decoratorType)
    {
        if (!this.decoratorsByVersionRange.TryGetValue(versionRange, out var types))
        {
            types = [];
            this.decoratorsByVersionRange[versionRange] = types;
        }

        types.Add(decoratorType);
    }

    /// <summary>
    /// Records that <paramref name="decoratorType"/> should run
    /// for all handlers. Global decorators are outermost in the chain.
    /// </summary>
    public void AddGlobal(Type decoratorType)
    {
        this.globalDecoratorTypes.Add(decoratorType);
    }

    /// <summary>
    /// Resolves decorators for the given handler type, filtered to those
    /// that implement <see cref="IHandlerDecorator{TRequest, TResponse}"/>
    /// matching the requested request/response types.
    /// Global decorators come first (outermost), followed by version-scoped decorators,
    /// then handler-specific decorators.
    /// </summary>
    public IReadOnlyList<IHandlerDecorator<TRequest, TResponse>> Resolve<TRequest, TResponse>(
        Type handlerType,
        SemVersionRange? versionRange,
        IServiceProvider serviceProvider)
    {
        var allDecoratorTypes = new List<Type>(this.globalDecoratorTypes);

        if (versionRange is not null && this.decoratorsByVersionRange.TryGetValue(versionRange, out var versionScoped))
        {
            allDecoratorTypes.AddRange(versionScoped);
        }

        if (this.decoratorsByHandlerType.TryGetValue(handlerType, out var handlerSpecific))
        {
            allDecoratorTypes.AddRange(handlerSpecific);
        }

        if (allDecoratorTypes.Count == 0)
        {
            return [];
        }

        var targetInterface = typeof(IHandlerDecorator<TRequest, TResponse>);

        return allDecoratorTypes
            .Where(targetInterface.IsAssignableFrom)
            .Select(type => (IHandlerDecorator<TRequest, TResponse>)serviceProvider.GetRequiredService(type))
            .ToArray();
    }
}
