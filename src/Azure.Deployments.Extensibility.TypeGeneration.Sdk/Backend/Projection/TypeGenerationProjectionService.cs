// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using Azure.Deployments.Extensibility.TypeGeneration.Backend.Validation;
using Azure.Deployments.Extensibility.TypeGeneration.Diagnostics;
using Azure.Deployments.Extensibility.TypeGeneration.Semantics;

namespace Azure.Deployments.Extensibility.TypeGeneration.Backend.Projection;

internal static class TypeGenerationProjectionService
{
    public static TypeGenerationProjectionResult Project(
        ExtensionTypeSemanticModel model,
        CancellationToken cancellationToken = default)
    {
        var diagnostics = TypeGenerationModelValidator.Validate(model, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        if (diagnostics.Any(diagnostic => diagnostic.Severity == TypeGenDiagnosticSeverity.Error))
        {
            return new(null, diagnostics);
        }

        var resources = ImmutableArray.CreateBuilder<ProjectedResourceTypeDefinition>();
        foreach (var resource in model.ResourceTypes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (resource.ApiVersions.IsEmpty)
            {
                resources.Add(ProjectResource(resource, null));
            }
            else
            {
                foreach (var version in resource.ApiVersions)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    resources.Add(ProjectResource(resource, version));
                }
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        return new(new TypeGenerationProjectedExtension(model, resources.ToImmutable()), diagnostics);
    }

    private static ProjectedResourceTypeDefinition ProjectResource(ResourceTypeDefinition resource, string? apiVersion) =>
        new()
        {
            Name = resource.Name,
            ApiVersion = apiVersion,
            ReadableScopes = resource.ReadableScopes,
            WritableScopes = resource.WritableScopes,
            Body = resource.Body,
            ResourceFunctions = [],
            SourceLocation = resource.SourceLocation,
        };
}
