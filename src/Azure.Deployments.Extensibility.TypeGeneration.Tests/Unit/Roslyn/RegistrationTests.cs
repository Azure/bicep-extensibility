// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.TypeGeneration.Diagnostics;
using Azure.Deployments.Extensibility.TypeGeneration.Roslyn;
using Azure.Deployments.Extensibility.TypeGeneration.Semantics;
using Azure.Deployments.Extensibility.TypeGeneration.Tests.Support;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Azure.Deployments.Extensibility.TypeGeneration.Tests.Unit.Roslyn;

[Trait("Category", "Unit")]
public class RegistrationTests
{
    private const string Import = "using Azure.Deployments.Extensibility.TypeGeneration.Attributes;\n";
    private static readonly ExtensionBuildMetadata Metadata = new("Contoso", "not-a-semver", true);

    [Fact]
    public void SingleResource_HasExplicitPhaseOneDefaults()
    {
        var result = Build("[ResourceType(\"Widgets\")] public class DifferentClrName { }");

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        var model = Assert.IsType<ExtensionTypeSemanticModel>(result.Model);
        Assert.Equal("Contoso", model.Name);
        Assert.Equal("not-a-semver", model.Version);
        Assert.True(model.IsSingleton);
        Assert.False(model.IsPreview);
        Assert.False(model.IsDeprecated);
        Assert.Null(model.Configuration);
        Assert.Null(model.FallbackResourceType);
        Assert.Null(model.SourceLocation);
        var resource = Assert.Single(model.ResourceTypes);
        Assert.Equal("Widgets", resource.Name);
        Assert.Empty(resource.ApiVersions);
        Assert.Equal(ResourceScope.All, resource.ReadableScopes);
        Assert.Equal(ResourceScope.All, resource.WritableScopes);
        Assert.Empty(resource.ResourceFunctions);
        var body = Assert.IsType<ObjectTypeDefinition>(resource.Body);
        Assert.Equal("DifferentClrName", body.Name);
        Assert.Empty(body.Properties);
        Assert.Null(body.AdditionalProperties);
        Assert.False(body.Sensitive);
        Assert.Equal("Registration.cs", resource.SourceLocation!.FilePath);
        Assert.Equal("Registration.cs", body.SourceLocation!.FilePath);
    }

    [Theory]
    [InlineData("", new string[0])]
    [InlineData(", null", new string[0])]
    [InlineData(", ApiVersions = new string[0]", new string[0])]
    [InlineData(", null, ApiVersions = new string[0]", new string[0])]
    [InlineData(", \"legacy\"", new[] { "legacy" })]
    [InlineData(", apiVersion: \"legacy\"", new[] { "legacy" })]
    [InlineData(", \"legacy\", ApiVersions = new string[0]", new[] { "legacy" })]
    [InlineData(", null, ApiVersions = new[] { \"v2\", \"v1\" }", new[] { "v2", "v1" })]
    [InlineData(", ApiVersions = new[] { \"v1\" }", new[] { "v1" })]
    [InlineData(", ApiVersions = new[] { \"v2\", \"v1\" }", new[] { "v2", "v1" })]
    [InlineData(", ApiVersions = new[] { \"V1\", \"v1\" }", new[] { "V1", "v1" })]
    [InlineData(", ApiVersions = new[] { \" label \" }", new[] { " label " })]
    public void VersionLabels_ArePreservedWithoutProjection(string arguments, string[] expected)
    {
        var result = Build($"[ResourceType(\"Widgets\"{arguments})] public class Widget {{ }}");

        Assert.True(result.Succeeded);
        var resource = Assert.Single(result.Model!.ResourceTypes);
        Assert.Equal<string>(expected, resource.ApiVersions);
        Assert.IsType<ObjectTypeDefinition>(resource.Body);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("\"\"")]
    [InlineData("\"   \"")]
    public void InvalidResourceName_HasExactAttributeSpan(string name)
    {
        var source = Import + $"[ResourceType({name})] public class Widget {{ }}";
        var diagnostic = Assert.Single(Errors(Analyze(source)));

        Assert.Equal(TypeGenDiagnosticCodes.InvalidResourceName, diagnostic.Code);
        Assert.Equal("Registration.cs", diagnostic.SourceLocation!.FilePath);
        Assert.Equal($"ResourceType({name})", source.Substring(
            diagnostic.SourceLocation.Start, diagnostic.SourceLocation.Length));
    }

    [Theory]
    [InlineData(", ApiVersions = null")]
    [InlineData(", ApiVersions = new string[] { null }")]
    [InlineData(", ApiVersions = new[] { \"\" }")]
    [InlineData(", ApiVersions = new[] { \"  \" }")]
    [InlineData(", ApiVersions = new[] { \"v1\", \"v1\" }")]
    [InlineData(", \"\"")]
    [InlineData(", \" \"")]
    public void InvalidVersionInput_FailsInsteadOfCleaningIt(string arguments)
    {
        var diagnostic = Assert.Single(Errors(Build($"[ResourceType(\"W\"{arguments})] public class Widget {{ }}")));

        Assert.Equal(TypeGenDiagnosticCodes.InvalidApiVersions, diagnostic.Code);
        Assert.NotNull(diagnostic.SourceLocation);
    }

    [Theory]
    [InlineData("\"legacy\"", "\"new\"")]
    [InlineData("\"legacy\"", "null")]
    [InlineData("\"\"", "\"new\"")]
    public void TwoVersionSources_AreRejectedEvenWhenAnElementIsInvalid(string legacy, string modern)
    {
        var diagnostics = Errors(Build(
            $"[ResourceType(\"W\", {legacy}, ApiVersions = new string[] {{ {modern} }})] public class Widget {{ }}"));

        Assert.Contains(diagnostics, diagnostic => diagnostic.Code == TypeGenDiagnosticCodes.ConflictingApiVersions);
        Assert.All(diagnostics, diagnostic => Assert.NotNull(diagnostic.SourceLocation));
    }

    [Theory]
    [InlineData("")]
    [InlineData(", ApiVersions = new[] { \"v1\" }")]
    public void DuplicateNames_AreRejectedForUnversionedAndDisjointVersions(string firstVersions)
    {
        var diagnostics = Errors(Build(
            $"[ResourceType(\"W\"{firstVersions})] public class First {{ }}\n" +
            "[ResourceType(\"W\", ApiVersions = new[] { \"v2\" })] public class Second { }"));

        Assert.Equal(2, diagnostics.Length);
        Assert.All(diagnostics, diagnostic =>
        {
            Assert.Equal(TypeGenDiagnosticCodes.DuplicateResourceName, diagnostic.Code);
            Assert.NotNull(diagnostic.SourceLocation);
        });
    }

    [Fact]
    public void UnversionedDuplicateNames_AreAlsoRejected()
    {
        var diagnostics = Errors(Build("""
            [ResourceType("W")] public class First { }
            [ResourceType("W")] public class Second { }
            """));
        Assert.Equal(2, diagnostics.Length);
        Assert.All(diagnostics, diagnostic => Assert.Equal(TypeGenDiagnosticCodes.DuplicateResourceName, diagnostic.Code));
    }

    [Fact]
    public void IndependentRegistrationErrors_AreCollectedTogether()
    {
        var diagnostics = Errors(Build("""
            [ResourceType("")] public class EmptyName { }
            [ResourceType("W", ApiVersions = new[] { "", "v1", "v1" })] public class First { }
            [ResourceType("W")] public class Second { }
            [BicepConfiguration] public class ConfigA { }
            [BicepConfiguration] public class ConfigB { }
            """));

        Assert.Equal(7, diagnostics.Length);
        Assert.Equal(1, diagnostics.Count(item => item.Code == TypeGenDiagnosticCodes.InvalidResourceName));
        Assert.Equal(2, diagnostics.Count(item => item.Code == TypeGenDiagnosticCodes.InvalidApiVersions));
        Assert.Equal(2, diagnostics.Count(item => item.Code == TypeGenDiagnosticCodes.DuplicateResourceName));
        Assert.Equal(2, diagnostics.Count(item => item.Code == TypeGenDiagnosticCodes.MultipleConfigurations));
        Assert.All(diagnostics, item => Assert.Equal("Registration.cs", item.SourceLocation!.FilePath));
    }

    [Fact]
    public void ConfigurationMarker_IsNotInherited()
    {
        var result = Build("""
            [BicepConfiguration] public class Settings { }
            public class DerivedSettings : Settings { }
            """);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Model!.ResourceTypes);
        Assert.Equal("Settings", Assert.IsType<ObjectTypeDefinition>(result.Model.Configuration).Name);
    }

    [Fact]
    public void Configuration_IsOptionalAndMayCoexistWithResourceMarker()
    {
        var result = Build("[ResourceType(\"W\"), BicepConfiguration] public class Shared { }");

        Assert.True(result.Succeeded);
        Assert.IsType<ObjectTypeDefinition>(result.Model!.Configuration);
        Assert.Single(result.Model.ResourceTypes);
    }

    [Fact]
    public void MultipleConfigurations_PointToBothExactMarkerSpans()
    {
        var source = Import + """
            [BicepConfiguration] public class First { }
            [BicepConfiguration] public class Second { }
            """;
        var diagnostics = Errors(Analyze(source));

        Assert.Equal(2, diagnostics.Length);
        Assert.All(diagnostics, diagnostic =>
        {
            Assert.Equal(TypeGenDiagnosticCodes.MultipleConfigurations, diagnostic.Code);
            var location = Assert.IsType<SourceLocation>(diagnostic.SourceLocation);
            Assert.Equal("BicepConfiguration", source.Substring(location.Start, location.Length));
        });
        Assert.NotEqual(diagnostics[0].SourceLocation!.Start, diagnostics[1].SourceLocation!.Start);
    }

    [Theory]
    [InlineData("[BicepConfiguration] public static class Settings { }", TypeGenDiagnosticCodes.UnsupportedSchema)]
    [InlineData("[BicepConfiguration] public class Settings<T> { }", TypeGenDiagnosticCodes.UnsupportedSchema)]
    [InlineData("[BicepConfiguration] public struct Settings { }", TypeGenDiagnosticCodes.CSharpCompilation)]
    [InlineData("[BicepConfiguration] public enum Settings { A }", TypeGenDiagnosticCodes.CSharpCompilation)]
    [InlineData("[ResourceType(\"W\")] public static class Widget { }", TypeGenDiagnosticCodes.UnsupportedSchema)]
    [InlineData("[ResourceType(\"W\")] public class Widget<T> { }", TypeGenDiagnosticCodes.UnsupportedSchema)]
    [InlineData("public class Outer<T> { [ResourceType(\"W\")] public class Widget { } }", TypeGenDiagnosticCodes.UnsupportedSchema)]
    public void InvalidRootShape_ReportsSemanticOrCompilerError(string declaration, string code)
    {
        var diagnostic = Assert.Single(Errors(Build(declaration)));
        Assert.Equal(code, diagnostic.Code);
        Assert.NotNull(diagnostic.SourceLocation);
    }

    [Theory]
    [InlineData("[ResourceType(\"W\")] public class Widget { public string Value { get; set; } = \"\"; }")]
    [InlineData("[BicepConfiguration] public class Settings { public int Value { get; } }")]
    [InlineData("public class Base { public int Value { get; set; } } [ResourceType(\"W\")] public class Widget : Base { }")]
    [InlineData("[ResourceType(\"W\")] public record Widget(string Name);")]
    public void SupportedPropertyMapping_ProducesThePreviouslyDeferredField(string declaration)
    {
        var result = Build(declaration);
        Assert.True(result.Succeeded);
        var body = Assert.IsType<ObjectTypeDefinition>(result.Model!.Configuration ??
            Assert.Single(result.Model.ResourceTypes).Body);
        var property = Assert.Single(body.Properties);
        Assert.True(property.Type is StringTypeDefinition or IntegerTypeDefinition);
    }

    [Fact]
    public void Indexer_RemainsAnExplicitErrorAfterTypeMappingIsAdded()
    {
        var diagnostic = Assert.Single(Errors(Build(
            "[ResourceType(\"W\")] public class Widget { public int this[int i] => i; }")));
        Assert.Equal(TypeGenDiagnosticCodes.UnsupportedIndexer, diagnostic.Code);
    }

    [Fact]
    public void NonSchemaMembers_AreNotMistakenForProperties()
    {
        var result = Build("""
            [ResourceType("W")] public abstract class Widget
            {
                public static int Global { get; set; }
                private int Hidden => 1;
                public int Field;
                public int Method() => Hidden;
            }
            """);
        Assert.True(result.Succeeded);
        Assert.Empty(Assert.IsType<ObjectTypeDefinition>(Assert.Single(result.Model!.ResourceTypes).Body).Properties);
    }

    [Fact]
    public void OnlyEffectivelyPublicTargetTypes_AreRegistered()
    {
        var result = Build("""
            [ResourceType("Internal")] internal class Internal { }
            internal class InternalOuter { [ResourceType("Hidden")] public class Nested { } }
            public class PublicOuter
            {
                [ResourceType("Private")] private class Private { }
                [ResourceType("Nested")] public class Nested { }
            }
            [BicepConfiguration] internal class HiddenConfig { }
            """);
        Assert.True(result.Succeeded);
        Assert.Equal("Nested", Assert.Single(result.Model!.ResourceTypes).Name);
        Assert.Null(result.Model.Configuration);
    }

    [Fact]
    public void InheritedResourceMarker_UsesDerivedDeclarationForLocation()
    {
        var reference = RoslynCompilationFactory.Create(
            Import + "[ResourceType(\"W\", \"legacy\")] public class Base { }").WithAssemblyName("ResourceLibrary");
        var result = new TypeGenerationFrontend().Build(
            RoslynCompilationFactory.Create(Import + "public class Derived : Base { }", "Derived.cs")
                .AddReferences(reference.ToMetadataReference()), Metadata);

        Assert.True(result.Succeeded);
        var resource = Assert.Single(result.Model!.ResourceTypes);
        Assert.Equal("W", resource.Name);
        Assert.Equal<string>(["legacy"], resource.ApiVersions);
        Assert.Equal("Derived.cs", resource.SourceLocation!.FilePath);
        Assert.Equal("Derived", Assert.IsType<ObjectTypeDefinition>(resource.Body).Name);
    }

    [Fact]
    public void InheritedAndExplicitResourceMarkers_FollowNearestDeclaration()
    {
        var result = Build("""
            [ResourceType("Base")] public class Base { }
            [ResourceType("Derived")] public class Derived : Base { }
            """);
        Assert.True(result.Succeeded);
        Assert.Equal(["Base", "Derived"], result.Model!.ResourceTypes.Select(resource => resource.Name));

        var errors = Errors(Build("""
            [ResourceType("Base")] public class Base { }
            public class Derived : Base { }
            """));
        Assert.Equal(2, errors.Length);
        Assert.All(errors, error => Assert.Equal(TypeGenDiagnosticCodes.DuplicateResourceName, error.Code));
    }

    [Fact]
    public void AliasAndFullyQualifiedSpelling_AreEquivalent()
    {
        var alias = Analyze("""
            using R = Azure.Deployments.Extensibility.TypeGeneration.Attributes.ResourceTypeAttribute;
            [R("W", ApiVersions = new[] { "v2", "v1" })] public class Widget { }
            """);
        var qualified = Analyze("""
            [global::Azure.Deployments.Extensibility.TypeGeneration.Attributes.ResourceTypeAttribute(
                "W", ApiVersions = new[] { "v2", "v1" })] public class Widget { }
            """);

        Assert.True(alias.Succeeded);
        Assert.True(qualified.Succeeded);
        Assert.True(SemanticModelComparer.AreEqual(alias.Model, qualified.Model));
    }

    [Fact]
    public void NestedPartialAndMultiFileTypes_RegisterExactlyOnceInOrdinalNameOrder()
    {
        var first = ("Z.cs", Import + """
            namespace Z;
            public partial class Outer
            {
                [ResourceType("z")] public partial class Nested { }
            }
            [ResourceType("a")] public class Last { }
            """);
        var second = ("A.cs", Import + """
            namespace Z;
            public partial class Outer { public partial class Nested { } }
            """);
        var third = ("Other.cs", Import + """
            namespace A;
            [ResourceType("B")] public class Middle { }
            """);
        var frontend = new TypeGenerationFrontend();
        var result = frontend.Build(RoslynCompilationFactory.Create(first, second, third), Metadata);
        var reordered = frontend.Build(RoslynCompilationFactory.Create(third, second, first), Metadata);

        Assert.True(result.Succeeded);
        Assert.True(reordered.Succeeded);
        Assert.Equal(["B", "a", "z"], result.Model!.ResourceTypes.Select(resource => resource.Name));
        Assert.True(SemanticModelComparer.AreEqual(result.Model, reordered.Model));
        Assert.Equal("Z.cs", result.Model.ResourceTypes[2].SourceLocation!.FilePath);
    }

    [Fact]
    public void ForeignShortNameAttributes_AndReferencedRegistrations_AreIgnored()
    {
        var library = RoslynCompilationFactory.Create(Import + """
            [ResourceType("Referenced")] public class Referenced { }
            [BicepConfiguration] public class ReferencedConfiguration { }
            namespace Foreign
            {
                [System.AttributeUsage(System.AttributeTargets.Class)]
                public class ResourceTypeAttribute(string name) : System.Attribute { }
                public class BicepConfigurationAttribute : System.Attribute { }
            }
            """).WithAssemblyName("ForeignLibrary");
        var target = RoslynCompilationFactory.Create("""
            [Foreign.ResourceType("Fake"), Foreign.BicepConfiguration] public class Fake { }
            """).AddReferences(library.ToMetadataReference());
        var result = new TypeGenerationFrontend().Build(target, Metadata);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Model!.ResourceTypes);
        Assert.Null(result.Model.Configuration);
    }

    [Fact]
    public void SameNamespaceForeignAttributes_AreNotAuthoringSymbols()
    {
        var result = Analyze("""
            namespace Foreign
            {
                public class ResourceTypeAttribute(string name) : System.Attribute { }
                public class BicepConfigurationAttribute : System.Attribute { }
                [ResourceType("Fake"), BicepConfiguration] public class Fake { }
            }
            """);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Model!.ResourceTypes);
        Assert.Null(result.Model.Configuration);
    }

    [Fact]
    public void CompilerWarnings_AreRetainedWithoutBlockingAnEmptyModel()
    {
        var result = Analyze("#warning fixture warning\n" + Import + "[ResourceType(\"W\")] public class Widget { }");

        Assert.True(result.Succeeded);
        var warning = Assert.Single(result.Diagnostics);
        Assert.Equal(TypeGenDiagnosticSeverity.Warning, warning.Severity);
        Assert.Contains("CS1030", warning.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CompilerErrors_StopDiscoveryWithoutCascadingRegistrationErrors()
    {
        var result = Build("[ResourceType(\"\")] public class Widget { public Missing Value { get; set; } }");
        Assert.All(Errors(result), diagnostic => Assert.Equal(TypeGenDiagnosticCodes.CSharpCompilation, diagnostic.Code));
    }

    [Fact]
    public void InvalidMetadata_StopsDiscovery()
    {
        var result = new TypeGenerationFrontend().Build(
            RoslynCompilationFactory.Create(Import + "[ResourceType(\"\")] public class Widget { }"),
            new("", ""));
        Assert.Equal([TypeGenDiagnosticCodes.InvalidExtensionName, TypeGenDiagnosticCodes.InvalidExtensionVersion],
            Errors(result).Select(diagnostic => diagnostic.Code));
    }

    [Fact]
    public void Frontend_UsesFreshStateOnEveryCall()
    {
        var frontend = new TypeGenerationFrontend();
        var invalid = frontend.Build(RoslynCompilationFactory.Create(
            Import + "[ResourceType(\"\")] public class Invalid { }"), Metadata);
        Assert.False(invalid.Succeeded);
        var valid = frontend.Build(RoslynCompilationFactory.Create(
            Import + "[ResourceType(\"W\")] public class Valid { }"), Metadata);
        Assert.True(valid.Succeeded);
        Assert.Empty(valid.Diagnostics);
        var empty = frontend.Build(RoslynCompilationFactory.Create("public class Unregistered { }"), new("Other", "2"));
        Assert.True(empty.Succeeded);
        Assert.Equal("Other", empty.Model!.Name);
        Assert.False(empty.Model.IsSingleton);
        Assert.Empty(empty.Model.ResourceTypes);
    }

    [Fact]
    public void Diagnostics_AreDeterministicAcrossSyntaxTreeOrder()
    {
        var first = ("Z.cs", Import + "[ResourceType(\"W\", ApiVersions = new[] { \"\", \"v\", \"v\" })] public class First { }");
        var second = ("A.cs", Import + "[ResourceType(\"W\")] public class Second { }");
        var frontend = new TypeGenerationFrontend();
        var a = frontend.Build(RoslynCompilationFactory.Create(first, second), Metadata);
        var b = frontend.Build(RoslynCompilationFactory.Create(second, first), Metadata);

        Assert.Equal(Errors(a), Errors(b));
        Assert.Equal("A.cs", a.Diagnostics[0].SourceLocation!.FilePath);
    }

    [Fact]
    public void Build_ValidatesArgumentsAndHonorsCancellation()
    {
        var frontend = new TypeGenerationFrontend();
        var compilation = RoslynCompilationFactory.Create("");
        Assert.Throws<ArgumentNullException>(() => frontend.Build(null!, Metadata));
        Assert.Throws<ArgumentNullException>(() => frontend.Build(compilation, null!));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => frontend.Build(compilation, Metadata, cancellation.Token));
    }

    private static TypeGenerationResult Build(string declaration) => Analyze(Import + declaration);

    private static TypeGenerationResult Analyze(string source)
        => new TypeGenerationFrontend().Build(RoslynCompilationFactory.Create(source, "Registration.cs"), Metadata);

    private static TypeGenDiagnostic[] Errors(TypeGenerationResult result)
    {
        Assert.False(result.Succeeded);
        Assert.Null(result.Model);
        var errors = result.Diagnostics.Where(diagnostic => diagnostic.Severity == TypeGenDiagnosticSeverity.Error).ToArray();
        Assert.NotEmpty(errors);
        return errors;
    }
}
