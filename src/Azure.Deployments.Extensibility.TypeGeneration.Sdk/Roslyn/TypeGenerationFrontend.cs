// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using Azure.Deployments.Extensibility.TypeGeneration.Diagnostics;
using Azure.Deployments.Extensibility.TypeGeneration.Roslyn.Discovery;
using Azure.Deployments.Extensibility.TypeGeneration.Roslyn.Types;
using Azure.Deployments.Extensibility.TypeGeneration.Roslyn.Validation;
using Azure.Deployments.Extensibility.TypeGeneration.Semantics;
using Microsoft.CodeAnalysis;

namespace Azure.Deployments.Extensibility.TypeGeneration.Roslyn;

public sealed class TypeGenerationFrontend : ITypeGenerationFrontend
{
    public TypeGenerationResult Build(
        Compilation compilation,
        ExtensionBuildMetadata metadata,
        CancellationToken cancellationToken = default)
    {
        var input = FrontendInputValidator.Validate(compilation, metadata, cancellationToken);
        if (input.Symbols is null)
        {
            return new(null, input.Diagnostics);
        }

        var diagnostics = input.Diagnostics.ToBuilder();
        var registrations = RegistrationDiscovery.Discover(compilation, input.Symbols, diagnostics, cancellationToken);
        var mapper = new TypeMapper(compilation, input.Symbols, diagnostics, cancellationToken);
        var resources = registrations.Resources.Select(resource =>
            (Registration: resource, Plan: mapper.ValidateRoot(resource.Schema, resource.SourceLocation))).ToArray();
        var configurations = registrations.Configurations.Select(configuration =>
            mapper.ValidateRoot(configuration.Schema, configuration.SourceLocation)).ToArray();

        cancellationToken.ThrowIfCancellationRequested();
        if (diagnostics.Any(diagnostic => diagnostic.Severity == TypeGenDiagnosticSeverity.Error))
        {
            return CreateResult(null, diagnostics);
        }

        // Materialize roots in validation order so completed subgraphs are reused in the same order.
        var resourceTypes = resources.Select(resource => new ResourceTypeDefinition
        {
            Name = resource.Registration.Name,
            ApiVersions = resource.Registration.ApiVersions,
            ReadableScopes = ResourceScope.All,
            WritableScopes = ResourceScope.All,
            Body = mapper.Build(resource.Plan),
            ResourceFunctions = [],
            SourceLocation = resource.Registration.SourceLocation,
        }).ToImmutableArray();
        var model = new ExtensionTypeSemanticModel
        {
            Name = metadata.Name,
            Version = metadata.Version,
            IsSingleton = metadata.IsSingleton,
            Configuration = configurations.Length == 0
                ? null
                : mapper.Build(configurations[0]),
            ResourceTypes = resourceTypes,
        };
        diagnostics.AddRange(PhaseOneModelValidator.Validate(model, cancellationToken));
        return CreateResult(model, diagnostics);
    }

    private static TypeGenerationResult CreateResult(
        ExtensionTypeSemanticModel? model,
        ImmutableArray<TypeGenDiagnostic>.Builder diagnostics)
        => new(diagnostics.Any(diagnostic => diagnostic.Severity == TypeGenDiagnosticSeverity.Error) ? null : model,
            diagnostics.OrderBy(diagnostic => diagnostic.SourceLocation?.FilePath ?? string.Empty, StringComparer.Ordinal)
                .ThenBy(diagnostic => diagnostic.SourceLocation?.Start ?? -1)
                .ThenBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
                .ThenBy(diagnostic => diagnostic.Message, StringComparer.Ordinal)
                .ToImmutableArray());
}
