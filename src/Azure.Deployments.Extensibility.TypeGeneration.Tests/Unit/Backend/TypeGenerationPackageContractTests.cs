// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Reflection;
using Azure.Deployments.Extensibility.TypeGeneration.Backend;
using Azure.Deployments.Extensibility.TypeGeneration.Backend.Validation;
using Azure.Deployments.Extensibility.TypeGeneration.Diagnostics;
using Azure.Deployments.Extensibility.TypeGeneration.Semantics;
using Xunit;

namespace Azure.Deployments.Extensibility.TypeGeneration.Tests.Unit.Backend;

[Trait("Category", "Unit")]
public class TypeGenerationPackageContractTests
{
    [Fact]
    public void ValidationEntryPoint_IsSynchronousAndIndependentOfProjectLoading()
    {
        var method = Assert.Single(typeof(TypeGenerationModelValidator).GetMethods(BindingFlags.Public | BindingFlags.Static));
        Assert.Equal("Validate", method.Name);
        Assert.Equal(typeof(ImmutableArray<TypeGenDiagnostic>), method.ReturnType);
        Assert.Equal([typeof(ExtensionTypeSemanticModel), typeof(CancellationToken)],
            method.GetParameters().Select(parameter => parameter.ParameterType));
        Assert.True(method.GetParameters()[1].IsOptional);
    }

    [Fact]
    public void Package_ExposesOnlyReadOnlyPayloads_AndCannotBeConstructedPublicly()
    {
        Assert.Empty(typeof(TypeGenerationPackage).GetConstructors());
        var properties = typeof(TypeGenerationPackage).GetProperties();
        Assert.Equal(["ArchiveBytes", "IndexJson", "TypesJson"], properties.Select(property => property.Name).Order(StringComparer.Ordinal));
        Assert.All(properties, property =>
        {
            Assert.Equal(typeof(ImmutableArray<byte>), property.PropertyType);
            Assert.Null(property.SetMethod);
        });
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    public void Package_RejectsMissingContent(int position, bool uninitialized)
    {
        ImmutableArray<byte>[] content = [[1], [2], [3]];
        content[position] = uninitialized ? default : [];
        var error = Assert.Throws<ArgumentException>(() => new TypeGenerationPackage(content[0], content[1], content[2]));
        Assert.Equal(new[] { "indexJson", "typesJson", "archiveBytes" }[position], error.ParamName);
    }

    [Fact]
    public void MissingPackage_IsNotSuccess()
    {
        Assert.False(new TypeGenerationPackageResult(null, []).Succeeded);
        var result = new TypeGenerationPackageResult(null,
            [new("TG2001", TypeGenDiagnosticSeverity.Error, "Invalid metadata.", null)]);
        Assert.False(result.Succeeded);
        Assert.Null(result.Package);
        Assert.Single(result.Diagnostics);
    }

    [Theory]
    [InlineData(TypeGenDiagnosticSeverity.Info)]
    [InlineData(TypeGenDiagnosticSeverity.Warning)]
    public void CompletePayload_AllowsNonErrorDiagnostics(TypeGenDiagnosticSeverity severity)
    {
        var package = new TypeGenerationPackage([1], [2], [3]);
        var result = new TypeGenerationPackageResult(package, [new("TG2001", severity, "Example.", null)]);
        Assert.True(result.Succeeded);
        Assert.Same(package, result.Package);
        Assert.True(new TypeGenerationPackageResult(package, []).Succeeded);
    }

    [Fact]
    public void ErrorResult_CannotCarryAPackage()
    {
        var error = Assert.Throws<ArgumentException>(() => new TypeGenerationPackageResult(
            new TypeGenerationPackage([1], [2], [3]),
            [new("TG2001", TypeGenDiagnosticSeverity.Error, "Invalid metadata.", null)]));
        Assert.Equal("package", error.ParamName);
    }

    [Fact]
    public void Result_RejectsUninitializedAndNullDiagnostics()
    {
        Assert.Throws<ArgumentException>(() => new TypeGenerationPackageResult(null, default));
        Assert.Throws<ArgumentException>(() => new TypeGenerationPackageResult(null, [null!]));
    }

    [Fact]
    public void BackendDiagnosticCodes_AreStableAndDoNotCollideWithFrontendCodes()
    {
        var codes = new[]
        {
            TypeGenDiagnosticCodes.BackendInvalidMetadata,
            TypeGenDiagnosticCodes.BackendInvalidResource,
            TypeGenDiagnosticCodes.BackendInvalidApiVersions,
            TypeGenDiagnosticCodes.BackendConflictingResources,
            TypeGenDiagnosticCodes.BackendInvalidRoot,
            TypeGenDiagnosticCodes.BackendInvalidGraph,
            TypeGenDiagnosticCodes.BackendInvalidProperty,
            TypeGenDiagnosticCodes.BackendInvalidPropertyFlags,
            TypeGenDiagnosticCodes.BackendInvalidScopes,
            TypeGenDiagnosticCodes.BackendInvalidConstraint,
            TypeGenDiagnosticCodes.BackendUnsupportedSemantic,
            TypeGenDiagnosticCodes.BackendUnsupportedType,
        };
        Assert.Equal(["TG2001", "TG2002", "TG2003", "TG2004", "TG2005", "TG2006",
            "TG2007", "TG2008", "TG2009", "TG2010", "TG2011", "TG2012"], codes);
        var all = typeof(TypeGenDiagnosticCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(field => (string)field.GetRawConstantValue()!).ToArray();
        Assert.Equal(all.Length, all.Distinct(StringComparer.Ordinal).Count());
    }
}
