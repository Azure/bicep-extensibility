// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using Azure.Deployments.Extensibility.TypeGeneration.Diagnostics;
using Azure.Deployments.Extensibility.TypeGeneration.Semantics;
using Microsoft.CodeAnalysis;

namespace Azure.Deployments.Extensibility.TypeGeneration.Roslyn.Discovery;

internal static class RegistrationDiscovery
{
    public static (ImmutableArray<ResourceRegistration> Resources, ImmutableArray<ConfigurationRegistration> Configurations) Discover(
        Compilation compilation,
        AuthoringSymbols symbols,
        ImmutableArray<TypeGenDiagnostic>.Builder diagnostics,
        CancellationToken cancellationToken)
    {
        var resources = ImmutableArray.CreateBuilder<ResourceRegistration>();
        var configurations = ImmutableArray.CreateBuilder<ConfigurationRegistration>();
        foreach (var type in EnumerateTypes(compilation.Assembly.GlobalNamespace))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsPubliclyVisible(type))
            {
                continue;
            }

            if (FindResourceAttribute(type, symbols.ResourceType) is { } resourceAttribute)
            {
                var location = GetLocation(type, resourceAttribute, cancellationToken);
                ReadResource(type, resourceAttribute, location, resources, diagnostics);
            }

            if (type.GetAttributes().FirstOrDefault(attribute =>
                SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, symbols.Configuration)) is { } configuration)
            {
                configurations.Add(new(type, GetLocation(type, configuration, cancellationToken)));
            }
        }

        var orderedResources = resources.OrderBy(resource => resource.Name, StringComparer.Ordinal)
            .ThenBy(resource => resource.Schema.ToDisplayString(), StringComparer.Ordinal)
            .ToImmutableArray();
        foreach (var group in orderedResources.GroupBy(resource => resource.Name, StringComparer.Ordinal)
            .Where(group => group.Count() > 1))
        {
            foreach (var resource in group)
            {
                diagnostics.Add(new(TypeGenDiagnosticCodes.DuplicateResourceName, TypeGenDiagnosticSeverity.Error,
                    $"Resource name '{resource.Name}' is registered more than once. Design Phase 1 permits one schema per resource name.",
                    resource.SourceLocation));
            }
        }

        var orderedConfigurations = configurations.OrderBy(
            configuration => configuration.Schema.ToDisplayString(), StringComparer.Ordinal).ToImmutableArray();
        if (orderedConfigurations.Length > 1)
        {
            foreach (var configuration in orderedConfigurations)
            {
                diagnostics.Add(new(TypeGenDiagnosticCodes.MultipleConfigurations, TypeGenDiagnosticSeverity.Error,
                    "An extension can declare at most one configuration class.", configuration.SourceLocation));
            }
        }

        return (orderedResources, orderedConfigurations);
    }

    private static void ReadResource(
        INamedTypeSymbol type,
        AttributeData attribute,
        SourceLocation? location,
        ImmutableArray<ResourceRegistration>.Builder resources,
        ImmutableArray<TypeGenDiagnostic>.Builder diagnostics)
    {
        var name = attribute.ConstructorArguments[0].Value as string;
        if (string.IsNullOrWhiteSpace(name))
        {
            diagnostics.Add(new(TypeGenDiagnosticCodes.InvalidResourceName, TypeGenDiagnosticSeverity.Error,
                "The public resource name cannot be null, empty, or whitespace.", location));
        }

        var legacyVersion = attribute.ConstructorArguments[1].Value as string;
        var versions = ImmutableArray.CreateBuilder<string>();
        var namedArray = attribute.NamedArguments.FirstOrDefault(argument => argument.Key == "ApiVersions");
        if (namedArray.Key is not null)
        {
            if (namedArray.Value.IsNull)
            {
                diagnostics.Add(new(TypeGenDiagnosticCodes.InvalidApiVersions, TypeGenDiagnosticSeverity.Error,
                    "ApiVersions cannot be null. Use an empty array for an unversioned resource.", location));
            }
            else
            {
                var seen = new HashSet<string>(StringComparer.Ordinal);
                foreach (var element in namedArray.Value.Values)
                {
                    if (element.Value is not string version || string.IsNullOrWhiteSpace(version))
                    {
                        diagnostics.Add(new(TypeGenDiagnosticCodes.InvalidApiVersions, TypeGenDiagnosticSeverity.Error,
                            "API version labels cannot be null, empty, or whitespace.", location));
                        continue;
                    }

                    if (!seen.Add(version))
                    {
                        diagnostics.Add(new(TypeGenDiagnosticCodes.InvalidApiVersions, TypeGenDiagnosticSeverity.Error,
                            $"API version '{version}' is listed more than once.", location));
                    }

                    versions.Add(version);
                }
            }
        }

        if (legacyVersion is not null)
        {
            if (namedArray.Key is not null && !namedArray.Value.IsNull && !namedArray.Value.Values.IsEmpty)
            {
                diagnostics.Add(new(TypeGenDiagnosticCodes.ConflictingApiVersions, TypeGenDiagnosticSeverity.Error,
                    "Use either the legacy apiVersion parameter or a non-empty ApiVersions array, not both.", location));
            }

            if (string.IsNullOrWhiteSpace(legacyVersion))
            {
                diagnostics.Add(new(TypeGenDiagnosticCodes.InvalidApiVersions, TypeGenDiagnosticSeverity.Error,
                    "The legacy apiVersion label cannot be empty or whitespace.", location));
            }
            else if (versions.Count == 0)
            {
                versions.Add(legacyVersion);
            }
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            resources.Add(new(name, versions.ToImmutable(), type, location));
        }
    }

    private static AttributeData? FindResourceAttribute(INamedTypeSymbol type, INamedTypeSymbol attributeType)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            var attribute = current.GetAttributes().FirstOrDefault(attribute =>
                SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType));
            if (attribute is not null)
            {
                return attribute;
            }
        }

        return null;
    }

    private static SourceLocation? GetLocation(
        INamedTypeSymbol type,
        AttributeData attribute,
        CancellationToken cancellationToken)
    {
        if (attribute.ApplicationSyntaxReference is { } syntax &&
            type.DeclaringSyntaxReferences.Any(declaration => declaration.SyntaxTree == syntax.SyntaxTree &&
                declaration.Span.Contains(syntax.Span)))
        {
            return SourceLocationConverter.FromLocation(syntax.GetSyntax(cancellationToken).GetLocation());
        }

        return SourceLocationConverter.FromSymbol(type);
    }

    private static bool IsPubliclyVisible(INamedTypeSymbol type)
    {
        for (var current = type; current is not null; current = current.ContainingType)
        {
            if (current.DeclaredAccessibility != Accessibility.Public)
            {
                return false;
            }
        }

        return true;
    }

    private static IEnumerable<INamedTypeSymbol> EnumerateTypes(INamespaceOrTypeSymbol container)
    {
        foreach (var member in container.GetMembers().OrderBy(member => member.MetadataName, StringComparer.Ordinal))
        {
            if (member is INamespaceSymbol space)
            {
                foreach (var nested in EnumerateTypes(space))
                {
                    yield return nested;
                }
            }
            else if (member is INamedTypeSymbol type)
            {
                yield return type;
                foreach (var nested in EnumerateTypes(type))
                {
                    yield return nested;
                }
            }
        }
    }
}
