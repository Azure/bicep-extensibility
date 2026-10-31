// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Globalization;
using Azure.Deployments.Extensibility.TypeGeneration.Attributes;
using Azure.Deployments.Extensibility.TypeGeneration.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Azure.Deployments.Extensibility.TypeGeneration.Roslyn;

internal static class FrontendInputValidator
{
    private const string SdkAssemblyName = "Azure.Deployments.Extensibility.TypeGeneration.Sdk";
    private const string AttributeNamespace = "Azure.Deployments.Extensibility.TypeGeneration.Attributes";

    public static (AuthoringSymbols? Symbols, ImmutableArray<TypeGenDiagnostic> Diagnostics) Validate(
        Compilation compilation,
        ExtensionBuildMetadata metadata,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(compilation);
        ArgumentNullException.ThrowIfNull(metadata);
        cancellationToken.ThrowIfCancellationRequested();

        var diagnostics = ImmutableArray.CreateBuilder<TypeGenDiagnostic>();
        if (string.IsNullOrWhiteSpace(metadata.Name))
        {
            diagnostics.Add(new(
                TypeGenDiagnosticCodes.InvalidExtensionName,
                TypeGenDiagnosticSeverity.Error,
                "The extension name is required.",
                null));
        }

        if (string.IsNullOrWhiteSpace(metadata.Version))
        {
            diagnostics.Add(new(
                TypeGenDiagnosticCodes.InvalidExtensionVersion,
                TypeGenDiagnosticSeverity.Error,
                "The extension version is required.",
                null));
        }

        var compilerDiagnostics = compilation.GetDiagnostics(cancellationToken)
            .Where(diagnostic => diagnostic.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning)
            .OrderBy(diagnostic => diagnostic.Location.SourceTree?.FilePath ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Location.SourceSpan.Start)
            .ThenBy(diagnostic => diagnostic.Id, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparer.Ordinal)
            .ToArray();

        foreach (var diagnostic in compilerDiagnostics)
        {
            cancellationToken.ThrowIfCancellationRequested();
            diagnostics.Add(new(
                TypeGenDiagnosticCodes.CSharpCompilation,
                diagnostic.Severity == DiagnosticSeverity.Error
                    ? TypeGenDiagnosticSeverity.Error
                    : TypeGenDiagnosticSeverity.Warning,
                $"{diagnostic.Id}: {diagnostic.GetMessage(CultureInfo.InvariantCulture)}",
                SourceLocationConverter.FromLocation(diagnostic.Location)));
        }

        if (compilerDiagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
        {
            return (null, diagnostics.ToImmutable());
        }

        var missing = new List<string>();
        var resource = ResolveType(compilation, $"{AttributeNamespace}.{nameof(ResourceTypeAttribute)}", SdkAssemblyName, TypeKind.Class, missing);
        var property = ResolveType(compilation, $"{AttributeNamespace}.{nameof(TypePropertyAttribute)}", SdkAssemblyName, TypeKind.Class, missing);
        var configuration = ResolveType(compilation, $"{AttributeNamespace}.{nameof(BicepConfigurationAttribute)}", SdkAssemblyName, TypeKind.Class, missing);
        var flags = ResolveType(compilation, "Azure.Bicep.Types.Concrete.ObjectTypePropertyFlags", "Azure.Bicep.Types", TypeKind.Enum, missing);
        cancellationToken.ThrowIfCancellationRequested();

        if (resource is null || property is null || configuration is null || flags is null)
        {
            diagnostics.Add(new(
                TypeGenDiagnosticCodes.InvalidAuthoringSdk,
                TypeGenDiagnosticSeverity.Error,
                $"Cannot resolve the required authoring types from their expected assemblies: {string.Join(", ", missing)}. Reference {SdkAssemblyName} and its Azure.Bicep.Types dependency.",
                null));
            return (null, diagnostics.ToImmutable());
        }

        if (diagnostics.Any(diagnostic => diagnostic.Severity == TypeGenDiagnosticSeverity.Error))
        {
            return (null, diagnostics.ToImmutable());
        }

        return (new AuthoringSymbols(resource, property, configuration, flags), diagnostics.ToImmutable());
    }

    private static INamedTypeSymbol? ResolveType(
        Compilation compilation,
        string metadataName,
        string assemblyName,
        TypeKind kind,
        List<string> missing)
    {
        var symbol = compilation.GetTypeByMetadataName(metadataName);
        if (symbol is null || symbol.TypeKind != kind || symbol.ContainingAssembly.Identity.Name != assemblyName)
        {
            missing.Add(metadataName);
            return null;
        }

        return symbol;
    }
}
