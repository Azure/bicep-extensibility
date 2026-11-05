// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Bicep.Types.Concrete;
using Azure.Deployments.Extensibility.TypeGeneration.Tests.Support;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Azure.Deployments.Extensibility.TypeGeneration.Tests.Unit.Attributes;

[Trait("Category", "Unit")]
public class AttributeCompilationTests
{
    private const string AttributeNamespace = "Azure.Deployments.Extensibility.TypeGeneration.Attributes";
    private const string SdkAssemblyName = "Azure.Deployments.Extensibility.TypeGeneration.Sdk";

    [Theory]
    [InlineData("""[ResourceType("Repository")]""", null, new string[0])]
    [InlineData("""[ResourceType("Repository", "v1")]""", "v1", new string[0])]
    [InlineData("""[ResourceType("Repository", apiVersion: "v1")]""", "v1", new string[0])]
    [InlineData("""[ResourceType("Repository", ApiVersions = new string[] { })]""", null, new string[0])]
    [InlineData("""[ResourceType("Repository", ApiVersions = new[] { "v1" })]""", null, new[] { "v1" })]
    [InlineData("""[ResourceType("Repository", ApiVersions = new[] { "v2", "v1" })]""", null, new[] { "v2", "v1" })]
    public void ResourceDeclarations_CompileAndPreserveAttributeArguments(
        string declaration,
        string? expectedLegacyVersion,
        string[] expectedVersions)
    {
        var compilation = RoslynCompilationFactory.Create($$"""
            using {{AttributeNamespace}};

            {{declaration}}
            public sealed class Repository { }
            """);

        AssertCompiles(compilation);
        var resource = GetRequiredType(compilation, "Repository");
        var attribute = Assert.Single(resource.GetAttributes());
        AssertSdkAttribute(compilation, attribute, "ResourceTypeAttribute");
        Assert.Equal("Repository", attribute.ConstructorArguments[0].Value);
        Assert.Equal(expectedLegacyVersion, attribute.ConstructorArguments[1].Value);

        var versionArguments = attribute.NamedArguments.Where(argument => argument.Key == "ApiVersions").ToArray();
        if (versionArguments.Length == 0)
        {
            Assert.Empty(expectedVersions);
        }
        else
        {
            var argument = Assert.Single(versionArguments).Value;
            Assert.Equal(TypedConstantKind.Array, argument.Kind);
            Assert.Equal(
                expectedVersions,
                argument.Values.Select(value => Assert.IsType<string>(value.Value)));
        }
    }

    [Fact]
    public void ModelWithInheritedIdentifierAndConfiguration_UsesTheNewSdkAttributes()
    {
        var compilation = RoslynCompilationFactory.Create($$"""
            using Azure.Bicep.Types.Concrete;
            using {{AttributeNamespace}};

            public class RepositoryIdentifiers
            {
                [TypeProperty("Repository name",
                    ObjectTypePropertyFlags.Required | ObjectTypePropertyFlags.Identifier)]
                public required string Name { get; init; }
            }

            [ResourceType("Repository", ApiVersions = new[] { "v1", "v2" })]
            public sealed class Repository : RepositoryIdentifiers
            {
                [TypeProperty("Description")]
                public string? Description { get; init; }

                [TypeProperty("URL", ObjectTypePropertyFlags.ReadOnly)]
                public string? Url { get; init; }
            }

            [BicepConfiguration]
            public sealed class Configuration
            {
                [TypeProperty("Token", flags: ObjectTypePropertyFlags.Required, isSecure: true)]
                public required string Token { get; init; }
            }
            """);

        AssertCompiles(compilation);
        var resource = GetRequiredType(compilation, "Repository");
        var identifiers = Assert.IsAssignableFrom<INamedTypeSymbol>(resource.BaseType);
        var name = Assert.IsAssignableFrom<IPropertySymbol>(Assert.Single(identifiers.GetMembers("Name")));
        var nameMetadata = Assert.Single(name.GetAttributes());
        AssertSdkAttribute(compilation, nameMetadata, "TypePropertyAttribute");
        Assert.Equal("Repository name", nameMetadata.ConstructorArguments[0].Value);
        Assert.Equal(
            (int)(ObjectTypePropertyFlags.Required | ObjectTypePropertyFlags.Identifier),
            Assert.IsType<int>(nameMetadata.ConstructorArguments[1].Value));

        var configuration = GetRequiredType(compilation, "Configuration");
        AssertSdkAttribute(compilation, Assert.Single(configuration.GetAttributes()), "BicepConfigurationAttribute");
        var token = Assert.IsAssignableFrom<IPropertySymbol>(Assert.Single(configuration.GetMembers("Token")));
        var tokenMetadata = Assert.Single(token.GetAttributes());
        AssertSdkAttribute(compilation, tokenMetadata, "TypePropertyAttribute");
        Assert.True(Assert.IsType<bool>(tokenMetadata.ConstructorArguments[2].Value));
        Assert.Empty(tokenMetadata.NamedArguments);
    }

    [Fact]
    public void AliasesAndLookalikes_HaveDifferentResolvedTypeIdentities()
    {
        var compilation = RoslynCompilationFactory.Create($$"""
            using System;
            using ResourceAlias = {{AttributeNamespace}}.ResourceTypeAttribute;

            [ResourceAlias("Repository")]
            public sealed class Repository { }

            [Foreign.ResourceType("NotRegistered")]
            public sealed class OtherResource { }

            namespace Foreign
            {
                [AttributeUsage(AttributeTargets.Class)]
                public sealed class ResourceTypeAttribute : Attribute
                {
                    public ResourceTypeAttribute(string name) { }
                }
            }
            """);

        AssertCompiles(compilation);
        var actual = Assert.Single(GetRequiredType(compilation, "Repository").GetAttributes());
        AssertSdkAttribute(compilation, actual, "ResourceTypeAttribute");
        var lookalike = Assert.Single(GetRequiredType(compilation, "OtherResource").GetAttributes());
        Assert.False(SymbolEqualityComparer.Default.Equals(actual.AttributeClass, lookalike.AttributeClass));
        Assert.Equal(
            compilation.Assembly,
            Assert.IsAssignableFrom<INamedTypeSymbol>(lookalike.AttributeClass).ContainingAssembly,
            SymbolEqualityComparer.Default);
    }

    [Theory]
    [InlineData("""
        [ResourceType("One"), ResourceType("Two")]
        public sealed class Model { }
        """, "CS0579")]
    [InlineData("""
        [BicepConfiguration, BicepConfiguration]
        public sealed class Model { }
        """, "CS0579")]
    [InlineData("""
        [TypeProperty("Not a property")]
        public sealed class Model { }
        """, "CS0592")]
    [InlineData("""
        public sealed class Model
        {
            [BicepConfiguration]
            public string Name { get; init; } = "";
        }
        """, "CS0592")]
    public void InvalidAttributePlacementOrDuplication_ProducesCompilerErrors(string source, string diagnosticId)
    {
        var compilation = RoslynCompilationFactory.Create(
            $"using {AttributeNamespace};{Environment.NewLine}{source}");

        var errors = compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Contains(errors, diagnostic => diagnostic.Id == diagnosticId);
    }

    private static void AssertCompiles(CSharpCompilation compilation)
    {
        var errors = compilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();

        Assert.True(errors.Length == 0, string.Join(Environment.NewLine, errors.Select(error => error.ToString())));
    }

    private static INamedTypeSymbol GetRequiredType(Compilation compilation, string metadataName)
        => compilation.GetTypeByMetadataName(metadataName)
            ?? throw new InvalidOperationException($"The fixture did not resolve '{metadataName}'.");

    private static void AssertSdkAttribute(Compilation compilation, AttributeData attribute, string name)
    {
        var expected = GetRequiredType(compilation, $"{AttributeNamespace}.{name}");
        Assert.True(SymbolEqualityComparer.Default.Equals(expected, attribute.AttributeClass));
        Assert.Equal(SdkAssemblyName, expected.ContainingAssembly.Identity.Name);
    }
}
