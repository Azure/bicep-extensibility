// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.TypeGeneration.Diagnostics;
using Azure.Deployments.Extensibility.TypeGeneration.Roslyn;
using Azure.Deployments.Extensibility.TypeGeneration.Roslyn.Validation;
using Azure.Deployments.Extensibility.TypeGeneration.Semantics;
using Azure.Deployments.Extensibility.TypeGeneration.Tests.Support;
using Xunit;

namespace Azure.Deployments.Extensibility.TypeGeneration.Tests.Unit.Roslyn;

[Trait("Category", "Unit")]
public class GraphRegressionTests
{
    private const string Imports = """
        using Azure.Deployments.Extensibility.TypeGeneration.Attributes;
        using Azure.Bicep.Types.Concrete;
        using System.Collections.Generic;

        """;

    [Fact]
    public void Contoso_GoldenMatchesTheCompleteTypedModel()
    {
        var result = BuildContoso(ContosoAuthoringFixture.Sources);
        Assert.True(result.Succeeded, string.Join(Environment.NewLine, result.Diagnostics));
        Assert.Empty(result.Diagnostics);
        Assert.True(SemanticModelComparer.AreEqual(ContosoAuthoringFixture.CreateExpected(), result.Model));
        Assert.Empty(PhaseOneModelValidator.Validate(result.Model!));
        var resource = Assert.Single(result.Model!.ResourceTypes);
        Assert.Equal(ResourceScope.All, resource.ReadableScopes);
        Assert.Equal(ResourceScope.All, resource.WritableScopes);
        Assert.Empty(resource.ResourceFunctions);
        Assert.Null(result.Model.FallbackResourceType);
        Assert.False(result.Model.IsPreview);
        Assert.False(result.Model.IsDeprecated);
        Assert.Equal(["description", "url", "name"],
            Assert.IsType<ObjectTypeDefinition>(resource.Body).Properties.Select(property => property.Name));
    }

    [Theory]
    [InlineData(0, 1, 2)]
    [InlineData(0, 2, 1)]
    [InlineData(1, 0, 2)]
    [InlineData(1, 2, 0)]
    [InlineData(2, 0, 1)]
    [InlineData(2, 1, 0)]
    public void SourceTreePermutations_KeepGoldenSemantics(int first, int second, int third)
    {
        var sources = ContosoAuthoringFixture.Sources;
        var result = BuildContoso([sources[first], sources[second], sources[third]]);
        Assert.True(result.Succeeded);
        Assert.True(SemanticModelComparer.AreEqual(ContosoAuthoringFixture.CreateExpected(), result.Model));
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void AliasAndFullyQualifiedAuthoring_MatchTheSameGolden()
    {
        var sources = ContosoAuthoringFixture.Sources.Select(source =>
            (source.Path, Source: source.Source
                .Replace("using Azure.Deployments.Extensibility.TypeGeneration.Attributes;",
                    """
                    using R = Azure.Deployments.Extensibility.TypeGeneration.Attributes.ResourceTypeAttribute;
                    using P = Azure.Deployments.Extensibility.TypeGeneration.Attributes.TypePropertyAttribute;
                    """, StringComparison.Ordinal)
                .Replace("[ResourceType(", "[R(", StringComparison.Ordinal)
                .Replace("[TypeProperty(", "[P(", StringComparison.Ordinal)
                .Replace("[BicepConfiguration]",
                    "[global::Azure.Deployments.Extensibility.TypeGeneration.Attributes.BicepConfigurationAttribute]", StringComparison.Ordinal)))
            .ToArray();
        var result = BuildContoso(sources);
        Assert.True(result.Succeeded);
        Assert.True(SemanticModelComparer.AreEqual(ContosoAuthoringFixture.CreateExpected(), result.Model));
    }

    [Fact]
    public void SourceLocations_AreExactAndIndependentOfSemanticComparison()
    {
        var sources = ContosoAuthoringFixture.Sources;
        var model = BuildContoso(sources).Model!;
        var resource = Assert.Single(model.ResourceTypes);
        Assert.Equal("ResourceType(\"Repository\", ApiVersions = new[] { \"2026-08-01\", \"2026-09-01\" })",
            TextAt(resource.SourceLocation!));
        var body = Assert.IsType<ObjectTypeDefinition>(resource.Body);
        Assert.Equal("Repository", TextAt(body.SourceLocation!));
        Assert.Equal("Description", TextAt(body.Properties[0].SourceLocation!));
        Assert.Equal("Url", TextAt(body.Properties[1].SourceLocation!));
        Assert.Equal("Name", TextAt(body.Properties[2].SourceLocation!));
        Assert.Equal("Identifiers.cs", body.Properties[2].SourceLocation!.FilePath);
        Assert.Equal("ContosoConfiguration", TextAt(model.Configuration!.SourceLocation!));
        Assert.True(SemanticModelComparer.AreEqual(ContosoAuthoringFixture.CreateExpected(), model));

        string TextAt(SourceLocation location)
            => sources.Single(source => source.Path == location.FilePath).Source.Substring(location.Start, location.Length);
    }

    [Fact]
    public void RepeatedBuilds_DoNotReuseObjectsAcrossCalls()
    {
        var frontend = new TypeGenerationFrontend();
        var compilation = RoslynCompilationFactory.Create(ContosoAuthoringFixture.Sources);
        ExtensionTypeSemanticModel? previous = null;
        for (var iteration = 0; iteration < 10; iteration++)
        {
            var result = frontend.Build(compilation, ContosoAuthoringFixture.Metadata);
            Assert.True(result.Succeeded);
            Assert.Empty(result.Diagnostics);
            Assert.True(SemanticModelComparer.AreEqual(ContosoAuthoringFixture.CreateExpected(), result.Model));
            if (previous is not null)
            {
                Assert.NotSame(previous.ResourceTypes[0].Body, result.Model!.ResourceTypes[0].Body);
            }
            previous = result.Model;
        }
    }

    [Fact]
    public async Task ConcurrentBuilds_KeepValidAndInvalidCompilationsIsolated()
    {
        var frontend = new TypeGenerationFrontend();
        var valid = RoslynCompilationFactory.Create(ContosoAuthoringFixture.Sources);
        var invalid = RoslynCompilationFactory.Create(Imports +
            "[ResourceType(\"Bad\")] public class Bad { public float Unsupported { get; set; } }");
        var results = await Task.WhenAll(Enumerable.Range(0, 16).Select(index => Task.Run(() =>
            frontend.Build(index % 2 == 0 ? valid : invalid, ContosoAuthoringFixture.Metadata))));

        for (var index = 0; index < results.Length; index++)
        {
            if (index % 2 == 0)
            {
                Assert.True(results[index].Succeeded);
                Assert.Empty(results[index].Diagnostics);
                Assert.True(SemanticModelComparer.AreEqual(ContosoAuthoringFixture.CreateExpected(), results[index].Model));
            }
            else
            {
                Assert.Null(results[index].Model);
                Assert.Equal(TypeGenDiagnosticCodes.UnsupportedType, Assert.Single(results[index].Diagnostics).Code);
            }
        }
    }

    [Fact]
    public void SameTypeNamesInDifferentCompilations_DoNotShareCachedShapes()
    {
        var frontend = new TypeGenerationFrontend();
        foreach (var type in new[] { "string", "int", "string" })
        {
            var compilation = RoslynCompilationFactory.Create(Imports +
                $"[ResourceType(\"R\")] public class Model {{ public {type} Value => default!; }}");
            var result = frontend.Build(compilation, new("E", "1"));
            Assert.True(result.Succeeded);
            var property = Assert.Single(Assert.IsType<ObjectTypeDefinition>(Assert.Single(result.Model!.ResourceTypes).Body).Properties);
            Assert.Equal(type == "string" ? typeof(StringTypeDefinition) : typeof(IntegerTypeDefinition), property.Type.GetType());
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SecurePlainOrder_DoesNotChangeSensitiveOccurrences(bool secureFirst)
    {
        const string secure = """
            [TypeProperty(null, isSecure: true)] public string SecureText => "";
            [TypeProperty(null, isSecure: true)] public List<string?>? SecureItems => null;
            [TypeProperty(null, isSecure: true)] public string[] SecureArray => [];
            """;
        const string plain = """
            public string PlainText => "";
            public List<string?>? PlainItems => null;
            public string[] PlainArray => [];
            """;
        var result = Analyze("[ResourceType(\"R\")] public class Model { " +
            (secureFirst ? secure + plain : plain + secure) + " }");
        Assert.True(result.Succeeded);
        var properties = Assert.IsType<ObjectTypeDefinition>(Assert.Single(result.Model!.ResourceTypes).Body).Properties;
        foreach (var property in properties)
        {
            Assert.Equal(property.Name.StartsWith("secure", StringComparison.Ordinal), StringLeaf(property.Type).Sensitive);
        }
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void SharedAndNullableOccurrences_RetainAllFieldsAndReferenceIdentity()
    {
        var result = Analyze("""
            public class Child { public string Value => ""; public int Count => 0; }
            [ResourceType("R")] public class Model
            {
                public Child First => new();
                public Child Second => new();
                public Child? Optional => null;
                public List<Child> Children => [];
                public List<Child?> NullableChildren => [];
            }
            """);
        Assert.True(result.Succeeded);
        var properties = Assert.IsType<ObjectTypeDefinition>(Assert.Single(result.Model!.ResourceTypes).Body).Properties;
        var child = Assert.IsType<ObjectTypeDefinition>(properties[0].Type);
        Assert.Equal(2, child.Properties.Length);
        Assert.Same(child, properties[1].Type);
        Assert.Same(child, NullableValue(properties[2].Type));
        Assert.Same(child, Assert.IsType<ArrayTypeDefinition>(properties[3].Type).ItemType);
        Assert.Same(child, NullableValue(Assert.IsType<ArrayTypeDefinition>(properties[4].Type).ItemType));
        Assert.Empty(PhaseOneModelValidator.Validate(result.Model!));
    }

    [Fact]
    public void CyclicGraph_MatchesAnIndependentExpectedGraphWithoutRecordEquality()
    {
        var result = Analyze("""
            public class Child { public Model Owner => null!; public string Value => ""; }
            [ResourceType("R")] public class Model
            {
                public Model? Parent => null;
                public Child Child => new();
                public List<Model> Children => [];
            }
            """);
        Assert.True(result.Succeeded);
        var expected = new ObjectTypeDefinition("Model", self =>
        {
            var child = new ObjectTypeDefinition
            {
                Name = "Child",
                Properties =
                [
                    new() { Name = "owner", Type = self },
                    new() { Name = "value", Type = new StringTypeDefinition() },
                ],
            };
            return ([
                new PropertyDefinition
                {
                    Name = "parent",
                    Type = new UnionTypeDefinition { Elements = [new() { Type = self }, new() { Type = new NullTypeDefinition() }] },
                },
                new PropertyDefinition { Name = "child", Type = child },
                new PropertyDefinition { Name = "children", Type = new ArrayTypeDefinition { ItemType = self } },
            ], null);
        });
        var body = Assert.IsType<ObjectTypeDefinition>(Assert.Single(result.Model!.ResourceTypes).Body);
        Assert.True(SemanticModelComparer.AreEqual(expected, body));
        Assert.Same(body, NullableValue(body.Properties[0].Type));
        Assert.Same(body, Assert.IsType<ObjectTypeDefinition>(body.Properties[1].Type).Properties[0].Type);
        Assert.Same(body, Assert.IsType<ArrayTypeDefinition>(body.Properties[2].Type).ItemType);
        Assert.Empty(PhaseOneModelValidator.Validate(result.Model!));
    }

    [Fact]
    public void CyclicCollectionAndMapNodes_AreSafeToMaterializeAndValidate()
    {
        var result = Analyze("""
            public class RecursiveList : List<RecursiveList> { }
            public class RecursiveMap : Dictionary<string, RecursiveMap> { }
            [ResourceType("R")] public class Model
            {
                public RecursiveList List => new();
                public RecursiveMap Map => new();
            }
            """);
        Assert.True(result.Succeeded);
        var properties = Assert.IsType<ObjectTypeDefinition>(Assert.Single(result.Model!.ResourceTypes).Body).Properties;
        var list = Assert.IsType<ArrayTypeDefinition>(properties[0].Type);
        var map = Assert.IsType<ObjectTypeDefinition>(properties[1].Type);
        Assert.Same(list, list.ItemType);
        Assert.Same(map, map.AdditionalProperties);
        Assert.Empty(PhaseOneModelValidator.Validate(result.Model!));
    }

    [Fact]
    public void RegistrationAndGraphErrors_AggregateInStableSourceOrder()
    {
        var first = ("Z.cs", Imports + """
            [ResourceType("Duplicate")] public class First { public float Bad => 0; }
            [BicepConfiguration] public class ConfigA { }
            """);
        var second = ("A.cs", Imports + """
            [ResourceType("Duplicate")] public class Second
            {
                [TypeProperty(null, (ObjectTypePropertyFlags)32)] public int WrongFlags => 0;
                public decimal Bad => 0;
                public Dictionary<int, string> WrongKey => [];
            }
            [BicepConfiguration] public class ConfigB { }
            """);
        var frontend = new TypeGenerationFrontend();
        var a = frontend.Build(RoslynCompilationFactory.Create(first, second), new("E", "1"));
        var b = frontend.Build(RoslynCompilationFactory.Create(second, first), new("E", "1"));
        Assert.False(a.Succeeded);
        Assert.Null(a.Model);
        Assert.Equal(8, a.Diagnostics.Length);
        Assert.Equal<TypeGenDiagnostic>(a.Diagnostics, b.Diagnostics);
        Assert.Equal("A.cs", a.Diagnostics[0].SourceLocation!.FilePath);
        Assert.Contains(a.Diagnostics, diagnostic => diagnostic.Code == TypeGenDiagnosticCodes.DuplicateResourceName);
        Assert.Contains(a.Diagnostics, diagnostic => diagnostic.Code == TypeGenDiagnosticCodes.MultipleConfigurations);
        Assert.Contains(a.Diagnostics, diagnostic => diagnostic.Code == TypeGenDiagnosticCodes.InvalidPropertyFlags);
        Assert.Contains(a.Diagnostics, diagnostic => diagnostic.Code == TypeGenDiagnosticCodes.InvalidDictionaryKey);
        Assert.Equal(2, a.Diagnostics.Count(diagnostic => diagnostic.Code == TypeGenDiagnosticCodes.UnsupportedType));
    }

    [Fact]
    public void BrokenCompilation_TakesPrecedenceOverSemanticGuesses()
    {
        var result = Analyze("""
            [ResourceType("")] public class Model
            {
                public MissingType Missing => default!;
                public float Unsupported => 0;
            }
            [BicepConfiguration] public class A { }
            [BicepConfiguration] public class B { }
            """);
        Assert.False(result.Succeeded);
        Assert.Null(result.Model);
        Assert.NotEmpty(result.Diagnostics);
        Assert.All(result.Diagnostics, diagnostic => Assert.Equal(TypeGenDiagnosticCodes.CSharpCompilation, diagnostic.Code));
    }

    [Fact]
    public void PartialDeclarations_RemainStableWhenSourceTreesAreReordered()
    {
        var first = ("Z.cs", Imports + """
            [ResourceType("R")] public partial class Model { public int Z => 0; }
            """);
        var second = ("A.cs", Imports + """
            public partial class Model { public string A => ""; }
            """);
        var frontend = new TypeGenerationFrontend();
        var a = frontend.Build(RoslynCompilationFactory.Create(first, second), new("E", "1"));
        var b = frontend.Build(RoslynCompilationFactory.Create(second, first), new("E", "1"));
        Assert.True(a.Succeeded);
        Assert.True(b.Succeeded);
        Assert.True(SemanticModelComparer.AreEqual(a.Model, b.Model));
        Assert.Equal(["a", "z"], Assert.IsType<ObjectTypeDefinition>(a.Model!.ResourceTypes[0].Body)
            .Properties.Select(property => property.Name));
    }

    [Fact]
    public void RepeatedUnsupportedTypes_ReportEachIndependentPropertyLocation()
    {
        var result = Analyze("""
            [ResourceType("R")] public class Model
            {
                public float First => 0;
                public float Second => 0;
                public List<float> Third => [];
                public List<float> Fourth => [];
            }
            """);
        Assert.False(result.Succeeded);
        Assert.Null(result.Model);
        Assert.Equal(4, result.Diagnostics.Length);
        Assert.All(result.Diagnostics, diagnostic => Assert.Equal(TypeGenDiagnosticCodes.UnsupportedType, diagnostic.Code));
        Assert.Equal(4, result.Diagnostics.Select(diagnostic => diagnostic.SourceLocation!.Start).Distinct().Count());
    }

    [Fact]
    public void CachedNullableCollectionErrors_AreReplayedAtEachUseSite()
    {
        var result = Analyze("""
            [ResourceType("R")] public class Model
            {
                public List<ulong?>?[] First => [];
                public List<ulong?>?[] Second => [];
                public List<ulong?>?[] Third => [];
            }
            """);
        Assert.Null(result.Model);
        Assert.Equal(3, result.Diagnostics.Length);
        Assert.All(result.Diagnostics, diagnostic => Assert.Equal(TypeGenDiagnosticCodes.UnsupportedIntegerRange, diagnostic.Code));
        Assert.Equal(3, result.Diagnostics.Select(diagnostic => diagnostic.SourceLocation!.Start).Distinct().Count());
    }

    [Fact]
    public void SharedInvalidObject_ReportsItsActualDeclarationOnlyOnce()
    {
        var result = Analyze("""
            public class Child { public float Bad => 0; }
            [ResourceType("R")] public class Model
            {
                public Child First => new();
                public Child Second => new();
                public Child? Optional => null;
            }
            """);
        Assert.Null(result.Model);
        Assert.Equal(TypeGenDiagnosticCodes.UnsupportedType, Assert.Single(result.Diagnostics).Code);
    }

    private static TypeGenerationResult BuildContoso((string Path, string Source)[] sources)
        => new TypeGenerationFrontend().Build(RoslynCompilationFactory.Create(sources), ContosoAuthoringFixture.Metadata);

    private static TypeGenerationResult Analyze(string source)
        => new TypeGenerationFrontend().Build(RoslynCompilationFactory.Create(Imports + source), new("E", "1"));

    private static TypeDefinition NullableValue(TypeDefinition type)
    {
        var union = Assert.IsType<UnionTypeDefinition>(type);
        Assert.IsType<NullTypeDefinition>(union.Elements[1].Type);
        return union.Elements[0].Type;
    }

    private static StringTypeDefinition StringLeaf(TypeDefinition type)
        => type switch
        {
            StringTypeDefinition text => text,
            ArrayTypeDefinition array => StringLeaf(array.ItemType),
            UnionTypeDefinition => StringLeaf(NullableValue(type)),
            _ => throw new InvalidOperationException($"Unexpected test type {type.GetType().Name}."),
        };
}
