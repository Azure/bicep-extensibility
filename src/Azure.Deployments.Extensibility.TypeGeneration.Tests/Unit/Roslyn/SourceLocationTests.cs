// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text;
using Azure.Deployments.Extensibility.TypeGeneration.Roslyn;
using Azure.Deployments.Extensibility.TypeGeneration.Semantics;
using Azure.Deployments.Extensibility.TypeGeneration.Tests.Support;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Azure.Deployments.Extensibility.TypeGeneration.Tests.Unit.Roslyn;

[Trait("Category", "Unit")]
public class SourceLocationTests
{
    [Fact]
    public void CompilerLocation_UsesUtf16NotBytesOrUnicodeScalarCount()
    {
        const string source = "// \U0001F9EA\npublic class Model { public Missing Value { get; set; } }";
        var result = FrontendInputValidator.Validate(
            RoslynCompilationFactory.Create(source, "unicode.cs"),
            new("Contoso", "1.2"));
        var diagnostic = Assert.Single(result.Diagnostics,
            diagnostic => diagnostic.Message.StartsWith("CS0246:", StringComparison.Ordinal));
        var location = Assert.IsType<SourceLocation>(diagnostic.SourceLocation);

        Assert.Equal("unicode.cs", location.FilePath);
        Assert.Equal(source.IndexOf("Missing", StringComparison.Ordinal), location.Start);
        Assert.Equal("Missing".Length, location.Length);
        Assert.NotEqual(Encoding.UTF8.GetByteCount(source.AsSpan(0, location.Start)), location.Start);
        Assert.NotEqual(source[..location.Start].EnumerateRunes().Count(), location.Start);
    }

    [Fact]
    public void LineDirectives_DoNotReplaceTheOriginalFileOrOffset()
    {
        const string source = "#line 200 \"mapped.cs\"\npublic class Model { public Missing Value { get; set; } }";
        var compilation = RoslynCompilationFactory.Create(source, "original.cs");
        var compilerDiagnostic = Assert.Single(compilation.GetDiagnostics(),
            diagnostic => diagnostic.Id == "CS0246");
        var location = SourceLocationConverter.FromLocation(compilerDiagnostic.Location);

        Assert.Equal("mapped.cs", compilerDiagnostic.Location.GetMappedLineSpan().Path);
        Assert.Equal("original.cs", location!.FilePath);
        Assert.Equal(source.IndexOf("Missing", StringComparison.Ordinal), location.Start);
    }

    [Fact]
    public void MetadataAndMissingLocations_DoNotInventSourceSpans()
    {
        var compilation = RoslynCompilationFactory.Create();
        var metadataLocation = Assert.Single(compilation.GetSpecialType(SpecialType.System_Object).Locations);

        Assert.Null(SourceLocationConverter.FromLocation(Location.None));
        Assert.Null(SourceLocationConverter.FromLocation(metadataLocation));
    }
}
