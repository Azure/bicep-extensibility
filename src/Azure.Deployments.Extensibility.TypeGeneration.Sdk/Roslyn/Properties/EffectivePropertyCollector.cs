// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using Azure.Deployments.Extensibility.TypeGeneration.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Azure.Deployments.Extensibility.TypeGeneration.Roslyn.Properties;

internal static class EffectivePropertyCollector
{
    public static ImmutableArray<EffectiveProperty> Collect(
        INamedTypeSymbol schema,
        AuthoringSymbols symbols,
        ImmutableArray<TypeGenDiagnostic>.Builder diagnostics,
        CancellationToken cancellationToken = default)
    {
        var properties = ImmutableArray.CreateBuilder<EffectiveProperty>();
        var hidingProperties = new List<IPropertySymbol>();
        for (var current = schema; current is not null; current = current.BaseType)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IEnumerable<IPropertySymbol> members = current.GetMembers().OfType<IPropertySymbol>();
            if (current.DeclaringSyntaxReferences.Length > 1)
            {
                // Partial declarations have no cross-file source order; choose paths without reordering within a file.
                members = members.OrderBy(property => SourceLocationConverter.FromSymbol(property)?.FilePath ?? string.Empty, StringComparer.Ordinal)
                    .ThenBy(property => SourceLocationConverter.FromSymbol(property)?.Start ?? -1);
            }

            foreach (var property in members)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (property.IsStatic ||
                    (!SymbolEqualityComparer.Default.Equals(current, schema) &&
                        property.DeclaredAccessibility == Accessibility.Private))
                {
                    continue;
                }

                if (hidingProperties.Any(derived => PropertySignatureComparer.HaveSameSignature(derived, property)))
                {
                    continue;
                }

                hidingProperties.Add(property);
                if (property.DeclaredAccessibility != Accessibility.Public)
                {
                    continue;
                }

                var location = SourceLocationConverter.FromSymbol(property) ?? SourceLocationConverter.FromSymbol(schema);
                if (property.IsIndexer)
                {
                    diagnostics.Add(new(TypeGenDiagnosticCodes.UnsupportedIndexer, TypeGenDiagnosticSeverity.Error,
                        $"Indexer '{property.Name}' cannot be exported as an object property.", location));
                    continue;
                }

                var metadata = TypePropertyReader.Read(property, symbols.TypeProperty, location, diagnostics, cancellationToken);
                properties.Add(new(property,
                    $"{property.Name[..1].ToLowerInvariant()}{property.Name[1..]}",
                    metadata.Description, metadata.Flags, metadata.IsSecure, location));
            }
        }

        foreach (var group in properties.GroupBy(property => property.Name, StringComparer.Ordinal)
            .Where(group => group.Count() > 1))
        {
            foreach (var property in group)
            {
                diagnostics.Add(new(TypeGenDiagnosticCodes.DuplicatePropertyName, TypeGenDiagnosticSeverity.Error,
                    $"Multiple effective properties export the name '{property.Name}'. Rename the conflicting CLR properties.",
                    property.SourceLocation));
            }
        }

        return properties.ToImmutable();
    }
}
