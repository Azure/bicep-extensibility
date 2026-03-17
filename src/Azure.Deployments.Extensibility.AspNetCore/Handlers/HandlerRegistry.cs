// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Semver;
using System.Diagnostics.CodeAnalysis;

namespace Azure.Deployments.Extensibility.AspNetCore.Handlers;

internal record HandlerRegistration(
    SemVersionRange VersionRange,
    Type HandlerInterface,
    Type HandlerType);

/// <summary>
/// The result of resolving a handler: the handler instance plus the version range it was matched under.
/// <see cref="MatchedVersionRange"/> is <c>null</c> for sentinel handlers (unknown version / unknown resource type).
/// </summary>
internal record HandlerResolution<THandler>(THandler Handler, SemVersionRange? MatchedVersionRange)
    where THandler : IHandler;

/// <summary>
/// Stores handler registrations grouped by extension version range.
/// Default handlers are stored separately from resource-type-specific handlers
/// (registered via <c>ForResourceType</c>).
/// </summary>
/// <remarks>
/// <see cref="ResolveHandler{THandler}"/> never throws. Instead, it returns an
/// <see cref="UnknownExtensionVersionHandler"/> or <see cref="UnknownResourceTypeHandler"/>
/// singleton so that pipeline behaviors (e.g., logging) still run for error cases.
/// </remarks>
public sealed class HandlerRegistry
{
    private static readonly Type[] DetectableInterfaces =
    [
        typeof(IResourcePreviewHandler),
        typeof(IResourceCreateOrUpdateHandler),
        typeof(IResourceGetHandler),
        typeof(IResourceDeleteHandler),
        typeof(ILongRunningOperationGetHandler),
    ];

    private readonly HashSet<SemVersionRange> registeredVersionRanges = [];
    private readonly List<HandlerRegistration> defaultHandlers = [];
    private readonly Dictionary<string, List<HandlerRegistration>> resourceTypeHandlers = new(StringComparer.OrdinalIgnoreCase);

    internal void TrackVersionRange(SemVersionRange versionRange)
    {
        this.registeredVersionRanges.Add(versionRange);
    }

    internal void AddDefault(SemVersionRange versionRange, Type handlerInterface, Type handlerType)
    {
        this.defaultHandlers.Add(new(versionRange, handlerInterface, handlerType));
    }

    internal void AddForResourceType(string resourceType, SemVersionRange versionRange, Type handlerInterface, Type handlerType)
    {
        if (!this.resourceTypeHandlers.TryGetValue(resourceType, out var list))
        {
            list = [];
            this.resourceTypeHandlers[resourceType] = list;
        }

        list.Add(new(versionRange, handlerInterface, handlerType));
    }

    /// <summary>
    /// Auto-detects handler interfaces implemented by <paramref name="handlerType"/> and
    /// registers them as default or resource-type-specific handlers.
    /// </summary>
    internal void Register(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type handlerType,
        SemVersionRange versionRange,
        string? resourceType)
    {
        foreach (var @interface in DetectableInterfaces)
        {
            if (@interface.IsAssignableFrom(handlerType))
            {
                if (resourceType is not null)
                {
                    this.AddForResourceType(resourceType, versionRange, @interface, handlerType);
                }
                else
                {
                    this.AddDefault(versionRange, @interface, handlerType);
                }
            }
        }
    }

    /// <summary>
    /// Resolves the handler for the given extension version and optional resource type.
    /// <list type="number">
    ///   <item>If no registration matches the extension version, returns <see cref="UnknownExtensionVersionHandler"/>.</item>
    ///   <item>If a resource-type-specific handler matches, it is returned.</item>
    ///   <item>Otherwise, falls back to a generic (default) handler.</item>
    ///   <item>If no generic handler is registered either, returns <see cref="UnknownResourceTypeHandler"/>.</item>
    /// </list>
    /// </summary>
    internal HandlerResolution<THandler> ResolveHandler<THandler>(
        string extensionVersion,
        string? resourceType,
        IServiceProvider serviceProvider)
        where THandler : IHandler
    {
        if (!SemVersion.TryParse(extensionVersion, out var targetVersion))
        {
            return new((THandler)(IHandler)new UnknownExtensionVersionHandler(extensionVersion), MatchedVersionRange: null);
        }

        var handlerInterfaceType = typeof(THandler);

        // Check whether any registration (default or resource-type-specific) matches the version.
        bool versionMatched = false;

        // 1. Try resource-type-specific match.
        if (resourceType is not null && this.resourceTypeHandlers.TryGetValue(resourceType, out var typeSpecificList))
        {
            var match = typeSpecificList
                .Where(r => r.HandlerInterface == handlerInterfaceType && r.VersionRange.Contains(targetVersion))
                .ToArray();

            if (match.Length == 1)
            {
                return new((THandler)serviceProvider.GetRequiredService(match[0].HandlerType), match[0].VersionRange);
            }

            if (match.Length > 1)
            {
                throw new InvalidOperationException(
                    $"Multiple handlers found for resource type '{resourceType}' and extension version '{extensionVersion}'.");
            }

            // Track that at least some registrations exist for this version (other interfaces).
            versionMatched = typeSpecificList.Any(r => r.VersionRange.Contains(targetVersion));
        }

        // 2. Fall back to default (generic) handlers.
        var defaults = this.defaultHandlers
            .Where(r => r.HandlerInterface == handlerInterfaceType && r.VersionRange.Contains(targetVersion))
            .ToArray();

        if (defaults.Length == 1)
        {
            return new((THandler)serviceProvider.GetRequiredService(defaults[0].HandlerType), defaults[0].VersionRange);
        }

        if (defaults.Length > 1)
        {
            throw new InvalidOperationException(
                $"Multiple default handlers found for extension version '{extensionVersion}'.");
        }

        // Check if the version matched any registration at all (for any handler interface).
        versionMatched = versionMatched
            || this.defaultHandlers.Any(r => r.VersionRange.Contains(targetVersion))
            || this.resourceTypeHandlers.Values.Any(list => list.Any(r => r.VersionRange.Contains(targetVersion)));

        // 3. No registration matched the version → unknown extension version.
        if (!versionMatched)
        {
            return new((THandler)(IHandler)new UnknownExtensionVersionHandler(extensionVersion), MatchedVersionRange: null);
        }

        // 4. Version matched but no handler for this resource type → unknown resource type.
        return new((THandler)(IHandler)new UnknownResourceTypeHandler(), MatchedVersionRange: null);
    }

    internal bool HasRegistration(Type handlerInterface)
    {
        return this.defaultHandlers.Any(r => r.HandlerInterface == handlerInterface)
            || this.resourceTypeHandlers.Values.Any(list => list.Any(r => r.HandlerInterface == handlerInterface));
    }

    /// <summary>
    /// Validates that every registered version range has at least one handler.
    /// Throws <see cref="InvalidOperationException"/> on the first empty range found.
    /// </summary>
    internal void Validate()
    {
        foreach (var range in this.registeredVersionRanges)
        {
            var hasDefault = this.defaultHandlers.Any(r => r.VersionRange == range);
            var hasResourceType = this.resourceTypeHandlers.Values.Any(list => list.Any(r => r.VersionRange == range));

            if (!hasDefault && !hasResourceType)
            {
                throw new InvalidOperationException(
                    $"Extension version range '{range}' was registered but has no handlers. " +
                    $"Call AddHandler inside the AddExtensionVersion callback to register at least one handler.");
            }
        }
    }
}
