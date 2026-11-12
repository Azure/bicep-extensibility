// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.TypeGeneration.Roslyn;
using Azure.Deployments.Extensibility.TypeGeneration.Tests.Support;
using Xunit;

namespace Azure.Deployments.Extensibility.TypeGeneration.Tests.Unit.Roslyn;

[Trait("Category", "Unit")]
public class AuthoringSymbolTests
{
    private const string AttributeNamespace = "Azure.Deployments.Extensibility.TypeGeneration.Attributes";

    [Fact]
    public void RealSdkSymbols_AreResolvedFromTheExpectedAssemblies()
    {
        var result = FrontendInputValidator.Validate(RoslynCompilationFactory.Create(), new("Contoso", "1.2"));
        var symbols = Assert.IsType<AuthoringSymbols>(result.Symbols);

        Assert.Equal("ResourceTypeAttribute", symbols.ResourceType.Name);
        Assert.Equal("TypePropertyAttribute", symbols.TypeProperty.Name);
        Assert.Equal("BicepConfigurationAttribute", symbols.Configuration.Name);
        Assert.Equal("Azure.Deployments.Extensibility.TypeGeneration.Sdk", symbols.ResourceType.ContainingAssembly.Identity.Name);
        Assert.Equal("Azure.Bicep.Types", symbols.PropertyFlags.ContainingAssembly.Identity.Name);
        Assert.Empty(result.Diagnostics);
    }

    [Theory]
    [InlineData("Azure.Deployments.Extensibility.TypeGeneration.Sdk.dll")]
    [InlineData("Azure.Bicep.Types.dll")]
    public void MissingReference_ProducesOneActionableDiagnostic(string fileName)
    {
        var compilation = RoslynCompilationFactory.Create();
        var reference = Assert.Single(compilation.References, reference =>
            Path.GetFileName(reference.Display) == fileName);
        compilation = compilation.RemoveReferences(reference);
        var result = FrontendInputValidator.Validate(compilation, new("Contoso", "1.2"));

        Assert.Null(result.Symbols);
        Assert.Equal("TG1102", Assert.Single(result.Diagnostics).Code);
        Assert.Contains("Reference Azure.Deployments.Extensibility.TypeGeneration.Sdk", result.Diagnostics[0].Message);
    }

    [Fact]
    public void SourceLookalike_CannotReplaceTheSdkContract()
    {
        var compilation = RoslynCompilationFactory.Create($$"""
            namespace {{AttributeNamespace}}
            {
                public class ResourceTypeAttribute : System.Attribute { }
            }
            """);
        var result = FrontendInputValidator.Validate(compilation, new("Contoso", "1.2"));

        Assert.Null(result.Symbols);
        Assert.Equal("TG1102", Assert.Single(result.Diagnostics).Code);
    }

    [Fact]
    public void AmbiguousReference_DoesNotReuseSymbolsFromAnEarlierValidation()
    {
        var valid = RoslynCompilationFactory.Create();
        Assert.NotNull(FrontendInputValidator.Validate(valid, new("Contoso", "1.2")).Symbols);

        var shadow = RoslynCompilationFactory.Create($$"""
            namespace {{AttributeNamespace}}
            {
                public class ResourceTypeAttribute : System.Attribute { }
            }
            """).WithAssemblyName("ShadowAuthoring");
        // This image is a negative-test reference, never the input extension's generated assembly.
        var ambiguous = valid.AddReferences(RoslynCompilationFactory.EmitReference(shadow));
        var result = FrontendInputValidator.Validate(ambiguous, new("Contoso", "1.2"));

        Assert.Null(result.Symbols);
        Assert.Equal("TG1102", Assert.Single(result.Diagnostics).Code);
        Assert.NotNull(FrontendInputValidator.Validate(valid, new("Contoso", "1.2")).Symbols);
    }
}
