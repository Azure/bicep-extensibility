// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using Azure.Deployments.Extensibility.TypeGeneration.Diagnostics;
using Azure.Deployments.Extensibility.TypeGeneration.Roslyn;
using Azure.Deployments.Extensibility.TypeGeneration.Roslyn.Properties;
using Azure.Deployments.Extensibility.TypeGeneration.Semantics;
using Azure.Deployments.Extensibility.TypeGeneration.Tests.Support;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Azure.Deployments.Extensibility.TypeGeneration.Tests.Unit.Roslyn;

[Trait("Category", "Unit")]
public class EffectivePropertyTests
{
    private const string Imports = """
        using Azure.Deployments.Extensibility.TypeGeneration.Attributes;
        using Azure.Bicep.Types.Concrete;

        """;

    public static IEnumerable<object[]> AllFlags => Enumerable.Range(0, 32).Select(value => new object[] { value });

    [Theory]
    [MemberData(nameof(AllFlags))]
    public void KnownFlagBitsAndEveryCombination_ArePreserved(int bits)
    {
        var result = Collect($$"""
            public class Model
            {
                [TypeProperty("description", (ObjectTypePropertyFlags){{bits}}, true)]
                public string Token { get; set; } = "";
            }
            """);
        Assert.Empty(result.Diagnostics);
        var property = Assert.Single(result.Properties);
        Assert.Equal((PropertyFlags)bits, property.Flags);
        Assert.Equal("description", property.Description);
        Assert.True(property.IsSecure);
    }

    [Theory]
    [InlineData(32)]
    [InlineData(33)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void UnknownFlagBits_AreErrorsAtTheExactAttribute(int bits)
    {
        var declaration = $$"""
            public class Model
            {
                [TypeProperty("description", (ObjectTypePropertyFlags)({{bits}}))]
                public string Name { get; set; } = "";
            }
            """;
        var result = Collect(declaration);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(TypeGenDiagnosticCodes.InvalidPropertyFlags, diagnostic.Code);
        var location = Assert.IsType<SourceLocation>(diagnostic.SourceLocation);
        Assert.Equal($"TypeProperty(\"description\", (ObjectTypePropertyFlags)({bits}))",
            (Imports + declaration).Substring(location.Start, location.Length));
    }

    [Fact]
    public void UnannotatedAndNullDescription_HaveDifferentDefaults()
    {
        var result = Collect("""
            public class Model
            {
                public string Plain { get; set; } = "";
                [TypeProperty(null)] public string Annotated { get; set; } = "";
            }
            """);
        Assert.Empty(result.Diagnostics);
        Assert.Null(result.Properties[0].Description);
        Assert.Equal(string.Empty, result.Properties[1].Description);
        Assert.All(result.Properties, property =>
        {
            Assert.Equal(PropertyFlags.None, property.Flags);
            Assert.False(property.IsSecure);
        });
    }

    [Fact]
    public void NamedConstructorArguments_AndAttributeAliasesAreRead()
    {
        var result = Collect("""
            using P = Azure.Deployments.Extensibility.TypeGeneration.Attributes.TypePropertyAttribute;
            public class Model
            {
                [P(isSecure: true, flags: ObjectTypePropertyFlags.Identifier, description: "Named")]
                public string Name { get; set; } = "";
            }
            """);
        var property = Assert.Single(result.Properties);
        Assert.Empty(result.Diagnostics);
        Assert.Equal("Named", property.Description);
        Assert.Equal(PropertyFlags.Identifier, property.Flags);
        Assert.True(property.IsSecure);
    }

    [Theory]
    [InlineData("public required string Value { get; init; }")]
    [InlineData("public string? Value { get; set; }")]
    [InlineData("public string Value { get; } = \"\";")]
    [InlineData("public string Value { get; private set; } = \"\";")]
    [InlineData("public string Value { private get; set; } = \"\";")]
    [InlineData("public string Value { set { } }")]
    public void CSharpRequiredNullableAndAccessors_DoNotInventBicepFlags(string declaration)
    {
        var result = Collect("public class Model { " + declaration + " }");
        Assert.Empty(result.Diagnostics);
        var property = Assert.Single(result.Properties);
        Assert.Equal(PropertyFlags.None, property.Flags);
        Assert.Null(property.Description);
    }

    [Theory]
    [InlineData("string")]
    [InlineData("string[]")]
    [InlineData("System.Collections.Generic.List<string>")]
    [InlineData("System.Collections.Generic.Dictionary<string, string>")]
    public void SecureMetadata_IsCarriedToTheFutureTypeMapper(string type)
    {
        var result = Collect($$"""
            public class Model
            {
                [TypeProperty(null, isSecure: true)]
                public {{type}} Token { get; set; } = default!;
            }
            """);
        Assert.Empty(result.Diagnostics);
        Assert.True(Assert.Single(result.Properties).IsSecure);
    }

    [Fact]
    public void Naming_LowercasesOnlyTheFirstCharacterAndIgnoresJsonAttributes()
    {
        var result = Collect("""
            public class Model
            {
                [System.Text.Json.Serialization.JsonPropertyName("renamed")]
                public string URL { get; set; } = "";
                [System.Text.Json.Serialization.JsonIgnore]
                public int IgnoredByJson { get; set; }
                public int AlreadyCamel { get; set; }
                public int X { get; set; }
                public int lower { get; set; }
            }
            """);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["uRL", "ignoredByJson", "alreadyCamel", "x", "lower"],
            result.Properties.Select(property => property.Name));
    }

    [Fact]
    public void RepositoryFixture_HasDerivedThenInheritedMetadataExactlyOnce()
    {
        var result = Collect("""
            public class Identifiers
            {
                [TypeProperty("Repository name", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
                public required string Name { get; set; }
            }
            public class Model : Identifiers
            {
                [TypeProperty("Repository description")] public string? Description { get; set; }
                [TypeProperty("Repository URL", ObjectTypePropertyFlags.ReadOnly)] public string? Url { get; set; }
            }
            """);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["description", "url", "name"], result.Properties.Select(property => property.Name));
        Assert.Equal(["Repository description", "Repository URL", "Repository name"],
            result.Properties.Select(property => property.Description));
        Assert.Equal([PropertyFlags.None, PropertyFlags.ReadOnly, PropertyFlags.Identifier | PropertyFlags.Required],
            result.Properties.Select(property => property.Flags));
        Assert.All(result.Properties, property => Assert.Equal("PropertyFixture.cs", property.SourceLocation!.FilePath));
    }

    [Fact]
    public void ConstructedGenericBaseMembers_UseSubstitutedTypes()
    {
        var result = Collect("""
            public class Base<T>
            {
                [TypeProperty("Value")] public T Value { get; set; } = default!;
                public System.Collections.Generic.List<T> Values { get; set; } = [];
            }
            public class Middle<U> : Base<System.Collections.Generic.List<U>> { }
            public class Model : Middle<string> { }
            """);
        Assert.Empty(result.Diagnostics);
        var valueType = Assert.IsAssignableFrom<INamedTypeSymbol>(result.Properties[0].Symbol.Type);
        Assert.Equal(SpecialType.System_String, Assert.Single(valueType.TypeArguments).SpecialType);
        var listType = Assert.IsAssignableFrom<INamedTypeSymbol>(result.Properties[1].Symbol.Type);
        Assert.True(SymbolEqualityComparer.Default.Equals(valueType, Assert.Single(listType.TypeArguments)));
        Assert.Equal("Value", result.Properties[0].Description);
    }

    [Fact]
    public void PositionalRecordProperties_AreNotLost()
    {
        var result = Collect("""
            public record Model(
                [property: TypeProperty("Record name", ObjectTypePropertyFlags.Identifier)] string Name,
                int Count)
            {
                public bool Extra { get; init; }
            }
            """);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["name", "count", "extra"], result.Properties.Select(property => property.Name));
        Assert.Equal("Record name", result.Properties[0].Description);
        Assert.Equal(PropertyFlags.Identifier, result.Properties[0].Flags);
    }

    [Fact]
    public void RecordInheritance_DoesNotDuplicatePositionalProperties()
    {
        var result = Collect("""
            public record Base([property: TypeProperty("Name")] string Name);
            public record Model(string Name, string Description) : Base(Name);
            """);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(["description", "name"], result.Properties.Select(property => property.Name));
        Assert.Equal("Name", result.Properties[1].Description);
    }

    [Fact]
    public void NonPublicStaticFieldsAndExplicitInterfaceMembers_AreExcluded()
    {
        var result = Collect("""
            public interface I { int Explicit { get; } }
            public class Model : I
            {
                [TypeProperty(null, (ObjectTypePropertyFlags)32)] public static int Global { get; set; }
                private int Private { get; set; }
                protected int Protected { get; set; }
                internal int Internal { get; set; }
                int I.Explicit => 0;
                public int Field;
                public int Value { get; set; }
            }
            """);
        Assert.Empty(result.Diagnostics);
        Assert.Equal("value", Assert.Single(result.Properties).Name);
    }

    [Theory]
    [InlineData("public int this[int index] => index;")]
    [InlineData("public int this[string index] => index.Length;")]
    public void Indexers_AreErrorsNotOrdinaryItemProperties(string declaration)
    {
        var result = Collect("public class Model { " + declaration + " }");
        Assert.Empty(result.Properties);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(TypeGenDiagnosticCodes.UnsupportedIndexer, diagnostic.Code);
        Assert.NotNull(diagnostic.SourceLocation);
    }

    [Fact]
    public void Override_InheritsMetadataFromTheNearestMarkedOverride()
    {
        var result = Collect("""
            public class Base
            {
                [TypeProperty("Base", ObjectTypePropertyFlags.Required)] public virtual string Name { get; set; } = "";
            }
            public class Middle : Base
            {
                [TypeProperty("Middle", ObjectTypePropertyFlags.Identifier, true)] public override string Name { get; set; } = "";
            }
            public class Model : Middle { public override string Name { get; set; } = ""; }
            """);
        var property = Assert.Single(result.Properties);
        Assert.Empty(result.Diagnostics);
        Assert.Equal("Middle", property.Description);
        Assert.Equal(PropertyFlags.Identifier, property.Flags);
        Assert.True(property.IsSecure);
    }

    [Fact]
    public void ExplicitOverrideMetadata_ReplacesRatherThanMergesTheBaseMetadata()
    {
        var result = Collect("""
            public class Base
            {
                [TypeProperty("Base", ObjectTypePropertyFlags.Required, true)] public virtual string Name { get; set; } = "";
            }
            public class Model : Base
            {
                [TypeProperty(null)] public override string Name { get; set; } = "";
            }
            """);
        var property = Assert.Single(result.Properties);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(string.Empty, property.Description);
        Assert.Equal(PropertyFlags.None, property.Flags);
        Assert.False(property.IsSecure);
    }

    [Fact]
    public void NewSameSignature_HidesBaseWithoutInheritingItsAttribute()
    {
        var result = Collect("""
            public class Base
            {
                [TypeProperty("Base", ObjectTypePropertyFlags.Required, true)] public string Name { get; set; } = "";
            }
            public class Model : Base { public new string Name { get; set; } = ""; }
            """);
        var property = Assert.Single(result.Properties);
        Assert.Empty(result.Diagnostics);
        Assert.Null(property.Description);
        Assert.Equal(PropertyFlags.None, property.Flags);
        Assert.False(property.IsSecure);
    }

    [Theory]
    [InlineData("public int URL { get; set; } public int uRL { get; set; }")]
    [InlineData("public int Name { get; set; } public int name { get; set; }")]
    public void ExportNameCollision_KeepsBothCandidatesAndReportsBothLocations(string declaration)
    {
        var result = Collect("public class Model { " + declaration + " }");
        Assert.Equal(2, result.Properties.Length);
        Assert.Equal(2, result.Diagnostics.Length);
        Assert.All(result.Diagnostics, diagnostic =>
        {
            Assert.Equal(TypeGenDiagnosticCodes.DuplicatePropertyName, diagnostic.Code);
            Assert.NotNull(diagnostic.SourceLocation);
        });
    }

    [Theory]
    [InlineData("public new string Name { get; set; } = \"\";")]
    [InlineData("public override string Name => \"\";")]
    public void DifferentReturnTypes_NeverSilentlyOverwriteAnExportedName(string declaration)
    {
        var result = Collect("""
            public class Base { public virtual object Name => new(); }
            public class Model : Base {
            """ + declaration + " }");
        Assert.Equal(2, result.Properties.Length);
        Assert.Equal(2, result.Diagnostics.Length);
        Assert.All(result.Diagnostics, diagnostic => Assert.Equal(TypeGenDiagnosticCodes.DuplicatePropertyName, diagnostic.Code));
    }

    [Fact]
    public void ForeignTypePropertyAttribute_IsNotInterpreted()
    {
        var result = Collect("""
            namespace Foreign
            {
                public class TypePropertyAttribute(string description) : System.Attribute { }
            }
            public class Model
            {
                [Foreign.TypeProperty("Fake")] public string Name { get; set; } = "";
            }
            """);
        Assert.Empty(result.Diagnostics);
        Assert.Null(Assert.Single(result.Properties).Description);
    }

    [Fact]
    public void ReferencedGenericBaseMetadata_IsReadWithCurrentSourceFallback()
    {
        var library = RoslynCompilationFactory.Create(Imports + """
            public class Base<T>
            {
                [TypeProperty("Base", ObjectTypePropertyFlags.Identifier, true)]
                public virtual T Name { get; set; } = default!;
            }
            """).WithAssemblyName("BaseLibrary");
        var compilation = RoslynCompilationFactory.Create(Imports + """
            public class Model : Base<string> { public override string Name { get; set; } = ""; }
            """, "Derived.cs").AddReferences(library.ToMetadataReference());
        var result = Collect(compilation);
        var property = Assert.Single(result.Properties);
        Assert.Empty(result.Diagnostics);
        Assert.Equal("Base", property.Description);
        Assert.Equal(PropertyFlags.Identifier, property.Flags);
        Assert.True(property.IsSecure);
        Assert.Equal(SpecialType.System_String, property.Symbol.Type.SpecialType);
        Assert.Equal("Derived.cs", property.SourceLocation!.FilePath);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RealMetadataReferences_UseTheCurrentRootWhenTheMemberHasNoSource(bool invalidFlags)
    {
        var flags = invalidFlags ? "(ObjectTypePropertyFlags)32" : "ObjectTypePropertyFlags.Identifier";
        var library = RoslynCompilationFactory.Create(Imports + $$"""
            public class Base<T>
            {
                [TypeProperty("Imported", {{flags}}, true)] public T Name { get; set; } = default!;
            }
            """).WithAssemblyName("MetadataBaseLibrary");
        var compilation = RoslynCompilationFactory.Create(
            "public class Model : Base<string> { }", "ImportedRoot.cs")
            .AddReferences(RoslynCompilationFactory.EmitReference(library));
        var result = Collect(compilation);
        var property = Assert.Single(result.Properties);

        Assert.DoesNotContain(property.Symbol.Locations, location => location.IsInSource);
        Assert.Equal("ImportedRoot.cs", property.SourceLocation!.FilePath);
        Assert.Equal(SpecialType.System_String, property.Symbol.Type.SpecialType);
        Assert.Equal("Imported", property.Description);
        Assert.True(property.IsSecure);
        if (invalidFlags)
        {
            var diagnostic = Assert.Single(result.Diagnostics);
            Assert.Equal(TypeGenDiagnosticCodes.InvalidPropertyFlags, diagnostic.Code);
            Assert.Equal(property.SourceLocation, diagnostic.SourceLocation);
        }
        else
        {
            Assert.Empty(result.Diagnostics);
            Assert.Equal(PropertyFlags.Identifier, property.Flags);
        }
    }

    [Theory]
    [InlineData("string", NullableAnnotation.NotAnnotated)]
    [InlineData("string?", NullableAnnotation.Annotated)]
    public void GenericSubstitution_RetainsReferenceNullability(string argument, NullableAnnotation annotation)
    {
        var result = Collect("""
            public class Base<T> { public T Value { get; set; } = default!; }
            """ + $"public class Model : Base<{argument}> {{ }}");

        Assert.Empty(result.Diagnostics);
        Assert.Equal(annotation, Assert.Single(result.Properties).Symbol.NullableAnnotation);
    }

    [Theory]
    [InlineData("public class Model { public int First { get; set; } public string Last { get; set; } = \"\"; }")]
    [InlineData("""
        public class Base
        {
            [TypeProperty("Base", ObjectTypePropertyFlags.Identifier, true)] public virtual string Name { get; set; } = "";
            public int Last { get; set; }
        }
        public class Model : Base { public int First { get; set; } public override string Name { get; set; } = ""; }
        """)]
    [InlineData("""
        public class Base { [TypeProperty("Base")] public int Value { get; set; } public int Last { get; set; } }
        public class Model : Base { public new int Value { get; set; } public int First { get; set; } }
        """)]
    [InlineData("""
        public class Base { public int P { get; set; } public int Q { get; set; } public int R { get; set; } public int S { get; set; } }
        public class Middle : Base { public new static int P { get; set; } protected new int Q { get; set; } private new string R { get; set; } private new int S { get; set; } }
        public class Model : Middle { }
        """)]
    [InlineData("""
        public class Base { public int P { get; set; } public int Q { get; set; } }
        public class Model : Base { private new int P { get; set; } protected new int Q { get; set; } }
        """)]
    [InlineData("public record Model([property: TypeProperty(\"Record\")] string Name, int Count) { public int Extra { get; set; } }")]
    [InlineData("""
        public record Base([property: TypeProperty("Name")] string Name);
        public record Model(string Name, int Count) : Base(Name);
        """)]
    [InlineData("""
        public class Base<T> { [TypeProperty("Base")] public T Value { get; set; } = default!; }
        public class Model : Base<string> { public int First { get; set; } }
        """)]
    [InlineData("""
        public class Base { [TypeProperty("Base")] public object Value { get; set; } = new(); }
        public class Model : Base { public new dynamic Value { get; set; } = new object(); }
        """)]
    [InlineData("""
        public class Base { [TypeProperty("Base")] public virtual object Value { get; set; } = new(); }
        public class Model : Base { public override dynamic Value { get; set; } = new object(); }
        """)]
    [InlineData("""
        public class Base { public object[] Value { get; set; } = []; }
        public class Model : Base { public new dynamic[] Value { get; set; } = []; }
        """)]
    [InlineData("""
        public class Base { public System.Collections.Generic.List<object[]> Value { get; set; } = []; }
        public class Model : Base { public new System.Collections.Generic.List<dynamic[]> Value { get; set; } = []; }
        """)]
    [InlineData("""
        public class Base { public (string A, int B) Value { get; set; } }
        public class Model : Base { public new (string C, int D) Value { get; set; } }
        """)]
    public void CompatibilityFixtures_MatchActualLegacyReflection(string declaration)
    {
        var compilation = RoslynCompilationFactory.Create(Imports + declaration, "Compatibility.cs");
        var result = Collect(compilation);
        Assert.Empty(result.Diagnostics);
        var expected = LegacyPropertyProbe.Read(compilation, "Model");
        var actual = result.Properties.Select(property =>
            (property.Name, property.Description, property.Flags, property.IsSecure)).ToArray();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Collector_HonorsCancellation()
    {
        var compilation = RoslynCompilationFactory.Create("public class Model { public int Value { get; set; } }");
        var input = FrontendInputValidator.Validate(compilation, new("E", "1"));
        Assert.NotNull(input.Symbols);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => EffectivePropertyCollector.Collect(
            compilation.GetTypeByMetadataName("Model")!, input.Symbols,
            ImmutableArray.CreateBuilder<TypeGenDiagnostic>(), cancellation.Token));
    }

    [Theory]
    [InlineData("public int URL { get; set; } public int uRL { get; set; }", TypeGenDiagnosticCodes.DuplicatePropertyName)]
    [InlineData("[TypeProperty(null, (ObjectTypePropertyFlags)32)] public int Value { get; set; }", TypeGenDiagnosticCodes.InvalidPropertyFlags)]
    [InlineData("public int this[int index] => index;", TypeGenDiagnosticCodes.UnsupportedIndexer)]
    [InlineData("public float Value { get; set; }", TypeGenDiagnosticCodes.UnsupportedType)]
    public void Build_WiresPropertyValidationAndRejectsUnsupportedTypes(string declaration, string code)
    {
        var compilation = RoslynCompilationFactory.Create(Imports +
            "[ResourceType(\"R\")] public class Model { " + declaration + " }");
        var result = new TypeGenerationFrontend().Build(compilation, new("E", "1"));
        Assert.False(result.Succeeded);
        Assert.Null(result.Model);
        Assert.NotEmpty(result.Diagnostics);
        Assert.All(result.Diagnostics, diagnostic => Assert.Equal(code, diagnostic.Code));
    }

    private static (ImmutableArray<EffectiveProperty> Properties, ImmutableArray<TypeGenDiagnostic> Diagnostics) Collect(
        string declaration) => Collect(RoslynCompilationFactory.Create(Imports + declaration, "PropertyFixture.cs"));

    private static (ImmutableArray<EffectiveProperty> Properties, ImmutableArray<TypeGenDiagnostic> Diagnostics) Collect(
        CSharpCompilation compilation)
    {
        var input = FrontendInputValidator.Validate(compilation, new("E", "1"));
        Assert.True(input.Symbols is not null, string.Join(Environment.NewLine, input.Diagnostics));
        var schema = compilation.GetTypeByMetadataName("Model");
        Assert.NotNull(schema);
        var diagnostics = ImmutableArray.CreateBuilder<TypeGenDiagnostic>();
        var properties = EffectivePropertyCollector.Collect(schema, input.Symbols, diagnostics);
        return (properties, diagnostics.ToImmutable());
    }
}
