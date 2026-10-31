// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.TypeGeneration.Diagnostics;
using Azure.Deployments.Extensibility.TypeGeneration.Roslyn;
using Azure.Deployments.Extensibility.TypeGeneration.Tests.Support;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Azure.Deployments.Extensibility.TypeGeneration.Tests.Unit.Roslyn;

[Trait("Category", "Unit")]
public class InputValidationTests
{
    [Theory]
    [InlineData(null, "1.2", "TG1001")]
    [InlineData("", "1.2", "TG1001")]
    [InlineData(" \t", "1.2", "TG1001")]
    [InlineData("Contoso", null, "TG1002")]
    [InlineData("Contoso", "", "TG1002")]
    [InlineData("Contoso", " \t", "TG1002")]
    public void MissingMetadata_ProducesAnErrorWithoutSourceLocation(string? name, string? version, string code)
    {
        var result = FrontendInputValidator.Validate(
            RoslynCompilationFactory.Create(),
            new ExtensionBuildMetadata(name!, version!));
        var diagnostic = Assert.Single(result.Diagnostics);

        Assert.Null(result.Symbols);
        Assert.Equal(code, diagnostic.Code);
        Assert.Equal(TypeGenDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Null(diagnostic.SourceLocation);
    }

    [Fact]
    public void IndependentMetadataErrors_AreReportedTogether()
    {
        var result = FrontendInputValidator.Validate(
            RoslynCompilationFactory.Create(),
            new ExtensionBuildMetadata("", ""));

        Assert.Null(result.Symbols);
        Assert.Equal(["TG1001", "TG1002"], result.Diagnostics.Select(diagnostic => diagnostic.Code));
    }

    [Theory]
    [InlineData("1.2")]
    [InlineData("1.2.3")]
    [InlineData("v1-preview")]
    public void ValidMetadata_IsPreservedWithoutAssumingSemver(string version)
    {
        var metadata = new ExtensionBuildMetadata("Contoso", version);
        var result = FrontendInputValidator.Validate(RoslynCompilationFactory.Create(), metadata);

        Assert.NotNull(result.Symbols);
        Assert.Empty(result.Diagnostics);
        Assert.Equal("Contoso", metadata.Name);
        Assert.Equal(version, metadata.Version);
        Assert.False(metadata.IsSingleton);
        Assert.True((metadata with { IsSingleton = true }).IsSingleton);
    }

    [Fact]
    public void CompilerErrors_PreserveOriginalCodeMessageAndLocation()
    {
        var compilation = RoslynCompilationFactory.Create(
            "public class Model { public Missing Value { get; set; } }",
            "model.cs");
        var result = FrontendInputValidator.Validate(compilation, new("Contoso", "1.2"));
        var diagnostic = Assert.Single(result.Diagnostics,
            diagnostic => diagnostic.Message.StartsWith("CS0246:", StringComparison.Ordinal));

        Assert.Null(result.Symbols);
        Assert.Equal(TypeGenDiagnosticCodes.CSharpCompilation, diagnostic.Code);
        Assert.Equal(TypeGenDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("Missing", diagnostic.Message);
        Assert.Equal("model.cs", diagnostic.SourceLocation!.FilePath);
    }

    [Fact]
    public void CompilerWarnings_DoNotBlockValidatedSymbols()
    {
        var result = FrontendInputValidator.Validate(
            RoslynCompilationFactory.Create("#warning example"),
            new("Contoso", "1.2"));
        var diagnostic = Assert.Single(result.Diagnostics);

        Assert.NotNull(result.Symbols);
        Assert.Equal(TypeGenDiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal("TG1101", diagnostic.Code);
        Assert.StartsWith("CS1030:", diagnostic.Message);
    }

    [Fact]
    public void ProjectWarningsAsErrors_AreStillCompilerErrors()
    {
        var compilation = RoslynCompilationFactory.Create("#warning example");
        compilation = compilation.WithOptions(
            compilation.Options.WithGeneralDiagnosticOption(ReportDiagnostic.Error));
        var result = FrontendInputValidator.Validate(compilation, new("Contoso", "1.2"));

        Assert.Null(result.Symbols);
        Assert.Equal(TypeGenDiagnosticSeverity.Error, Assert.Single(result.Diagnostics).Severity);
    }

    [Fact]
    public void CompilerErrors_DoNotCauseSecondaryGuessedContractErrors()
    {
        var compilation = RoslynCompilationFactory.Create("public class Broken {");
        var reference = Assert.Single(compilation.References, reference =>
            Path.GetFileName(reference.Display) == "Azure.Deployments.Extensibility.TypeGeneration.Sdk.dll");
        compilation = compilation.RemoveReferences(reference);
        var result = FrontendInputValidator.Validate(compilation, new("", "1.2"));

        Assert.Null(result.Symbols);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "TG1001");
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "TG1101");
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Code == "TG1102");
    }

    [Fact]
    public void DiagnosticOrder_DoesNotDependOnSourceTreeEnumeration()
    {
        var first = RoslynCompilationFactory.Create(("z.cs", "#warning z"), ("a.cs", "#warning a"));
        var second = RoslynCompilationFactory.Create(("a.cs", "#warning a"), ("z.cs", "#warning z"));
        var expected = FrontendInputValidator.Validate(first, new("Contoso", "1.2"));
        var actual = FrontendInputValidator.Validate(second, new("Contoso", "1.2"));

        Assert.Equal<TypeGenDiagnostic>(expected.Diagnostics, actual.Diagnostics);
        Assert.Equal(["a.cs", "z.cs"], actual.Diagnostics.Select(diagnostic => diagnostic.SourceLocation!.FilePath));
    }

    [Fact]
    public void PreCancelledValidation_ThrowsCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var exception = Assert.Throws<OperationCanceledException>(() =>
            FrontendInputValidator.Validate(RoslynCompilationFactory.Create(), new("Contoso", "1.2"), cancellation.Token));
        Assert.Equal(cancellation.Token, exception.CancellationToken);
    }

    [Fact]
    public void MissingApiArguments_AreReportedAsProgrammingErrors()
    {
        Assert.Throws<ArgumentNullException>(() => FrontendInputValidator.Validate(null!, new("Contoso", "1.2")));
        Assert.Throws<ArgumentNullException>(() => FrontendInputValidator.Validate(RoslynCompilationFactory.Create(), null!));
    }

    [Fact]
    public async Task ConcurrentValidations_DoNotShareErrorState()
    {
        var compilation = RoslynCompilationFactory.Create();
        var results = await Task.WhenAll(Enumerable.Range(0, 16).Select(index => Task.Run(() =>
            FrontendInputValidator.Validate(
                compilation,
                new ExtensionBuildMetadata(index % 2 == 0 ? "Contoso" : "", "1.2")))));

        for (var index = 0; index < results.Length; index++)
        {
            if (index % 2 == 0)
            {
                Assert.NotNull(results[index].Symbols);
                Assert.Empty(results[index].Diagnostics);
            }
            else
            {
                Assert.Null(results[index].Symbols);
                Assert.Equal("TG1001", Assert.Single(results[index].Diagnostics).Code);
            }
        }
    }
}
