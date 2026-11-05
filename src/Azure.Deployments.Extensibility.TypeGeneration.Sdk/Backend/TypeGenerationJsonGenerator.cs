// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using Azure.Deployments.Extensibility.TypeGeneration.Backend.Lowering;
using Azure.Deployments.Extensibility.TypeGeneration.Backend.Projection;
using Azure.Deployments.Extensibility.TypeGeneration.Backend.Serialization;
using Azure.Deployments.Extensibility.TypeGeneration.Backend.Validation;
using Azure.Deployments.Extensibility.TypeGeneration.Diagnostics;
using Azure.Deployments.Extensibility.TypeGeneration.Semantics;

namespace Azure.Deployments.Extensibility.TypeGeneration.Backend;

internal static class TypeGenerationJsonGenerator
{
    public static TypeGenerationValidationResult Generate(
        ExtensionTypeSemanticModel model,
        CancellationToken cancellationToken = default)
    {
        var diagnostics = ImmutableArray.CreateBuilder<TypeGenDiagnostic>();
        var projection = TypeGenerationProjectionService.Project(model, cancellationToken);
        diagnostics.AddRange(projection.Diagnostics);
        if (!projection.Succeeded) { return new(null, TypeGenerationModelValidator.OrderDiagnostics(diagnostics)); }

        var lowering = TypeGenerationLowerer.Lower(projection.Model!, cancellationToken);
        diagnostics.AddRange(lowering.Diagnostics);
        if (!lowering.Succeeded) { return new(null, TypeGenerationModelValidator.OrderDiagnostics(diagnostics)); }

        var serialization = TypeGenerationJsonWriter.Write(lowering.Model!, cancellationToken);
        diagnostics.AddRange(serialization.Diagnostics);
        if (!serialization.Succeeded) { return new(null, TypeGenerationModelValidator.OrderDiagnostics(diagnostics)); }

        var validation = TypeGenerationPackageValidator.Validate(serialization.Model!, projection.Model, cancellationToken);
        diagnostics.AddRange(validation.Diagnostics);
        cancellationToken.ThrowIfCancellationRequested();
        return new(validation.Model, TypeGenerationModelValidator.OrderDiagnostics(diagnostics));
    }
}
