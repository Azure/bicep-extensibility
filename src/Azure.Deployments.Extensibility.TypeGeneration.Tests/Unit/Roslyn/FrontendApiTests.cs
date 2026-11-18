// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using Azure.Deployments.Extensibility.TypeGeneration.Diagnostics;
using Azure.Deployments.Extensibility.TypeGeneration.Roslyn;
using Azure.Deployments.Extensibility.TypeGeneration.Tests.Support;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Azure.Deployments.Extensibility.TypeGeneration.Tests.Unit.Roslyn;

[Trait("Category", "Unit")]
public class FrontendApiTests
{
    [Fact]
    public void BuildInterface_HasTheAgreedSynchronousSignature()
    {
        var method = Assert.Single(typeof(ITypeGenerationFrontend).GetMethods());
        var parameters = method.GetParameters();

        Assert.Equal("Build", method.Name);
        Assert.Equal(typeof(TypeGenerationResult), method.ReturnType);
        Assert.Equal([typeof(Compilation), typeof(ExtensionBuildMetadata), typeof(CancellationToken)],
            parameters.Select(parameter => parameter.ParameterType));
        Assert.True(parameters[2].IsOptional);
    }

    [Fact]
    public void ResultWithoutModel_IsNotSuccessful()
    {
        Assert.False(new TypeGenerationResult(null, []).Succeeded);
        var result = new TypeGenerationResult(null,
            [new("TG1001", TypeGenDiagnosticSeverity.Error, "Missing name.", null)]);

        Assert.False(result.Succeeded);
        Assert.Null(result.Model);
    }

    [Theory]
    [InlineData(TypeGenDiagnosticSeverity.Info)]
    [InlineData(TypeGenDiagnosticSeverity.Warning)]
    public void NonErrorDiagnostics_DoNotMakeACompleteResultFail(TypeGenDiagnosticSeverity severity)
    {
        var model = SemanticModelFixtures.CreateContoso();
        var result = new TypeGenerationResult(model, [new("TG1101", severity, "Example.", null)]);

        Assert.True(result.Succeeded);
        Assert.Same(model, result.Model);
        Assert.True(new TypeGenerationResult(model, []).Succeeded);
    }

    [Fact]
    public void ErrorResult_CannotExposeAPartialModel()
    {
        Assert.Throws<ArgumentException>(() => new TypeGenerationResult(
            SemanticModelFixtures.CreateContoso(),
            [new("TG1101", TypeGenDiagnosticSeverity.Error, "Compiler error.", null)]));
    }

    [Fact]
    public void Result_RejectsUninitializedOrNullDiagnostics()
    {
        Assert.Throws<ArgumentException>(() => new TypeGenerationResult(null, default));
        Assert.Throws<ArgumentException>(() => new TypeGenerationResult(null, [null!]));
    }

    [Fact]
    public void PublicResultAndDiagnostics_DoNotExposeRoslynTypes()
    {
        var types = new[] { typeof(TypeGenerationResult), typeof(TypeGenDiagnostic), typeof(ExtensionBuildMetadata) };
        foreach (var property in types.SelectMany(type => type.GetProperties()))
        {
            Assert.False(property.PropertyType.Namespace?.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal) == true);
            Assert.DoesNotContain(property.PropertyType.GetGenericArguments(),
                argument => argument.Namespace?.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal) == true);
        }

        Assert.Equal(typeof(ImmutableArray<TypeGenDiagnostic>),
            typeof(TypeGenerationResult).GetProperty(nameof(TypeGenerationResult.Diagnostics))!.PropertyType);
    }

    [Fact]
    public void DiagnosticCodes_AreStableAndDistinct()
    {
        var codes = new[]
        {
            TypeGenDiagnosticCodes.InvalidExtensionName,
            TypeGenDiagnosticCodes.InvalidExtensionVersion,
            TypeGenDiagnosticCodes.CSharpCompilation,
            TypeGenDiagnosticCodes.InvalidAuthoringSdk,
            TypeGenDiagnosticCodes.InvalidResourceName,
            TypeGenDiagnosticCodes.InvalidApiVersions,
            TypeGenDiagnosticCodes.ConflictingApiVersions,
            TypeGenDiagnosticCodes.DuplicateResourceName,
            TypeGenDiagnosticCodes.MultipleConfigurations,
            TypeGenDiagnosticCodes.UnsupportedSchema,
            TypeGenDiagnosticCodes.DuplicatePropertyName,
            TypeGenDiagnosticCodes.InvalidPropertyFlags,
            TypeGenDiagnosticCodes.UnsupportedIndexer,
            TypeGenDiagnosticCodes.UnsupportedType,
            TypeGenDiagnosticCodes.UnsupportedIntegerRange,
            TypeGenDiagnosticCodes.InvalidDictionaryKey,
            TypeGenDiagnosticCodes.AmbiguousCollection,
            TypeGenDiagnosticCodes.ObliviousNullability,
            TypeGenDiagnosticCodes.TypeGraphTooDeep,
            TypeGenDiagnosticCodes.InvalidSemanticModel,
        };

        Assert.Equal(["TG1001", "TG1002", "TG1101", "TG1102", "TG1201", "TG1202", "TG1203", "TG1204", "TG1205", "TG1301", "TG1401", "TG1402", "TG1403",
            "TG1501", "TG1502", "TG1503", "TG1504", "TG1505", "TG1506", "TG1601"], codes);
        Assert.Equal(codes.Length, codes.Distinct(StringComparer.Ordinal).Count());
    }
}
