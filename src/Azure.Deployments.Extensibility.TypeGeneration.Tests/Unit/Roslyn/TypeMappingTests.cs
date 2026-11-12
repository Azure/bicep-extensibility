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
public class TypeMappingTests
{
    private const string Imports = """
        using Azure.Deployments.Extensibility.TypeGeneration.Attributes;
        using Azure.Bicep.Types.Concrete;
        using System.Collections.Generic;

        """;

    [Theory]
    [InlineData("sbyte", (long)sbyte.MinValue, (long)sbyte.MaxValue)]
    [InlineData("byte", (long)byte.MinValue, (long)byte.MaxValue)]
    [InlineData("short", (long)short.MinValue, (long)short.MaxValue)]
    [InlineData("ushort", (long)ushort.MinValue, (long)ushort.MaxValue)]
    [InlineData("int", (long)int.MinValue, (long)int.MaxValue)]
    [InlineData("uint", (long)uint.MinValue, (long)uint.MaxValue)]
    [InlineData("long", long.MinValue, long.MaxValue)]
    public void Integers_HaveExactClrBounds(string type, long min, long max)
    {
        var integer = Assert.IsType<IntegerTypeDefinition>(Property(type));
        Assert.Equal(min, integer.MinValue);
        Assert.Equal(max, integer.MaxValue);
    }

    [Theory]
    [InlineData("sbyte?")]
    [InlineData("byte?")]
    [InlineData("short?")]
    [InlineData("ushort?")]
    [InlineData("int?")]
    [InlineData("uint?")]
    [InlineData("long?")]
    public void NullableIntegers_KeepTheirUnderlyingBounds(string type)
    {
        var value = Assert.IsType<IntegerTypeDefinition>(NullableValue(Property(type)));
        var nonNullable = Assert.IsType<IntegerTypeDefinition>(Property(type[..^1]));
        Assert.Equal(nonNullable.MinValue, value.MinValue);
        Assert.Equal(nonNullable.MaxValue, value.MaxValue);
    }

    [Fact]
    public void StringAndBoolean_HaveNoInventedConstraints()
    {
        var text = Assert.IsType<StringTypeDefinition>(Property("string"));
        Assert.False(text.Sensitive);
        Assert.Null(text.MinLength);
        Assert.Null(text.MaxLength);
        Assert.Null(text.Pattern);
        Assert.IsType<BooleanTypeDefinition>(Property("bool"));
        Assert.IsType<StringTypeDefinition>(NullableValue(Property("string?")));
        Assert.IsType<BooleanTypeDefinition>(NullableValue(Property("bool?")));
    }

    [Theory]
    [InlineData("string[]")]
    [InlineData("List<string>")]
    [InlineData("IEnumerable<string>")]
    [InlineData("IList<string>")]
    [InlineData("ICollection<string>")]
    [InlineData("IReadOnlyList<string>")]
    [InlineData("IReadOnlyCollection<string>")]
    [InlineData("HashSet<string>")]
    [InlineData("ISet<string>")]
    [InlineData("IReadOnlySet<string>")]
    [InlineData("Queue<string>")]
    [InlineData("Stack<string>")]
    [InlineData("LinkedList<string>")]
    [InlineData("System.Collections.Immutable.ImmutableArray<string>")]
    [InlineData("System.Collections.Immutable.ImmutableList<string>")]
    [InlineData("System.Collections.Immutable.IImmutableList<string>")]
    [InlineData("System.Collections.ObjectModel.Collection<string>")]
    public void SupportedCollections_NormalizeToArray(string type)
    {
        var array = Assert.IsType<ArrayTypeDefinition>(Property(type));
        Assert.IsType<StringTypeDefinition>(array.ItemType);
        Assert.Null(array.MinLength);
        Assert.Null(array.MaxLength);
    }

    [Fact]
    public void NullableCollectionAndNullableElements_AreIndependent()
    {
        var nullableItems = Assert.IsType<ArrayTypeDefinition>(NullableValue(Property("List<string?>?")));
        Assert.IsType<StringTypeDefinition>(NullableValue(nullableItems.ItemType));
        var requiredItems = Assert.IsType<ArrayTypeDefinition>(NullableValue(Property("List<string>?")));
        Assert.IsType<StringTypeDefinition>(requiredItems.ItemType);
        Assert.False(SemanticModelComparer.AreEqual(nullableItems, requiredItems));
        var requiredCollection = Assert.IsType<ArrayTypeDefinition>(Property("string?[]"));
        Assert.IsType<StringTypeDefinition>(NullableValue(requiredCollection.ItemType));
        var nullableCollection = Assert.IsType<ArrayTypeDefinition>(NullableValue(Property("string[]?")));
        Assert.IsType<StringTypeDefinition>(nullableCollection.ItemType);
    }

    [Theory]
    [InlineData("Dictionary<string, string?>")]
    [InlineData("IDictionary<string, string?>")]
    [InlineData("IReadOnlyDictionary<string, string?>")]
    [InlineData("SortedDictionary<string, string?>")]
    [InlineData("SortedList<string, string?>")]
    [InlineData("System.Collections.Immutable.ImmutableDictionary<string, string?>")]
    [InlineData("System.Collections.Immutable.IImmutableDictionary<string, string?>")]
    [InlineData("System.Collections.ObjectModel.ReadOnlyDictionary<string, string?>")]
    public void StringKeyMaps_NormalizeToAdditionalProperties(string type)
    {
        var map = Assert.IsType<ObjectTypeDefinition>(Property(type));
        Assert.Empty(map.Properties);
        Assert.False(map.Sensitive);
        Assert.IsType<StringTypeDefinition>(NullableValue(Assert.IsAssignableFrom<TypeDefinition>(map.AdditionalProperties)));
        Assert.StartsWith("Map:", map.Name, StringComparison.Ordinal);
    }

    [Fact]
    public void NestedCollectionsMapsAndNullableValues_ArePreservedAtEveryLevel()
    {
        var outer = Assert.IsType<ArrayTypeDefinition>(NullableValue(
            Property("List<Dictionary<string, List<int?>?>?>?")));
        var map = Assert.IsType<ObjectTypeDefinition>(NullableValue(outer.ItemType));
        var inner = Assert.IsType<ArrayTypeDefinition>(NullableValue(map.AdditionalProperties!));
        var integer = Assert.IsType<IntegerTypeDefinition>(NullableValue(inner.ItemType));
        Assert.Equal(int.MinValue, integer.MinValue);
        Assert.Equal(int.MaxValue, integer.MaxValue);
    }

    [Fact]
    public void NestedObjectsAndClosedGenerics_KeepDistinctArgumentShapes()
    {
        var result = Analyze("""
            public class Box<T> { public T Value { get; set; } = default!; }
            public class Base<T> { public T Inherited { get; set; } = default!; }
            [ResourceType("R")] public class Model : Base<List<string?>>
            {
                public Box<string> Text { get; set; } = new();
                public Box<int> Number { get; set; } = new();
                public Box<string?> NullableText { get; set; } = new();
                public Box<string>? NullableBox { get; set; }
            }
            """);
        var properties = Body(result).Properties;
        Assert.Equal(["text", "number", "nullableText", "nullableBox", "inherited"], properties.Select(property => property.Name));
        var text = Assert.IsType<ObjectTypeDefinition>(properties[0].Type);
        var number = Assert.IsType<ObjectTypeDefinition>(properties[1].Type);
        var nullableText = Assert.IsType<ObjectTypeDefinition>(properties[2].Type);
        Assert.IsType<StringTypeDefinition>(Assert.Single(text.Properties).Type);
        Assert.IsType<IntegerTypeDefinition>(Assert.Single(number.Properties).Type);
        Assert.IsType<StringTypeDefinition>(NullableValue(Assert.Single(nullableText.Properties).Type));
        Assert.Same(text, NullableValue(properties[3].Type));
        Assert.IsType<StringTypeDefinition>(NullableValue(Assert.IsType<ArrayTypeDefinition>(properties[4].Type).ItemType));
        Assert.NotSame(text, nullableText);
    }

    [Fact]
    public void SameSimpleNames_DoNotCollideInTypeCacheOrSyntheticMapNames()
    {
        var result = Analyze("""
            namespace A { public class Item { public int Number { get; set; } public Dictionary<string, int> Map { get; set; } = []; } }
            namespace B { public class Item { public string Text { get; set; } = ""; public Dictionary<string, bool> Map { get; set; } = []; } }
            [ResourceType("R")] public class Model
            {
                public A.Item First { get; set; } = new();
                public B.Item Second { get; set; } = new();
            }
            """);
        var properties = Body(result).Properties;
        var first = Assert.IsType<ObjectTypeDefinition>(properties[0].Type);
        var second = Assert.IsType<ObjectTypeDefinition>(properties[1].Type);
        Assert.NotSame(first, second);
        Assert.IsType<IntegerTypeDefinition>(first.Properties[0].Type);
        Assert.IsType<StringTypeDefinition>(second.Properties[0].Type);
        Assert.NotEqual(Assert.IsType<ObjectTypeDefinition>(first.Properties[1].Type).Name,
            Assert.IsType<ObjectTypeDefinition>(second.Properties[1].Type).Name);
    }

    [Fact]
    public void Enum_UsesRawNamesInUnsignedNumericOrderAndIgnoresSerializerNames()
    {
        var result = Analyze("""
            [System.Flags] public enum Mode : int
            {
                [System.Runtime.Serialization.EnumMember(Value = "renamed")]
                Negative = -1,
                Later = 2,
                [System.Text.Json.Serialization.JsonStringEnumMemberName("lowered")]
                First = 0,
                Alias = 2
            }
            [ResourceType("R")] public class Model { public Mode Value { get; set; } public Mode? Optional { get; set; } }
            """);
        var body = Body(result);
        var union = Assert.IsType<UnionTypeDefinition>(body.Properties[0].Type);
        Assert.Equal(["First", "Later", "Alias", "Negative"], union.Elements.Select(
            element => Assert.IsType<StringLiteralTypeDefinition>(element.Type).Value));
        Assert.Same(union, NullableValue(body.Properties[1].Type));
        Assert.All(union.Elements, element =>
        {
            Assert.Null(element.Availability.AddedAt);
            Assert.Null(element.Availability.RemovedAt);
        });
    }

    [Fact]
    public void UnsignedLongEnum_ExportsNamesRatherThanRejectingItsNumericRange()
    {
        var result = Analyze("""
            public enum Wide : ulong { Max = ulong.MaxValue, Zero = 0 }
            [ResourceType("R")] public class Model { public Wide Value { get; set; } }
            """);
        Assert.Equal(["Zero", "Max"], Assert.IsType<UnionTypeDefinition>(Assert.Single(Body(result).Properties).Type)
            .Elements.Select(element => Assert.IsType<StringLiteralTypeDefinition>(element.Type).Value));
    }

    [Theory]
    [InlineData("float")]
    [InlineData("double")]
    [InlineData("decimal")]
    [InlineData("char")]
    [InlineData("nint")]
    [InlineData("nuint")]
    [InlineData("object")]
    [InlineData("dynamic")]
    [InlineData("System.DateTime")]
    [InlineData("System.Guid")]
    [InlineData("(int A, string B)")]
    [InlineData("System.Action")]
    [InlineData("System.Span<int>")]
    [InlineData("System.Memory<int>")]
    [InlineData("int[,]")]
    [InlineData("int[,,]")]
    [InlineData("System.Collections.IEnumerable")]
    [InlineData("System.Collections.Hashtable")]
    public void UnsupportedShapes_AreErrorsNotAnyOrEmptyObjects(string type)
    {
        var result = Analyze($"[ResourceType(\"R\")] public class Model {{ public {type} Value => default!; }}");
        AssertError(result, TypeGenDiagnosticCodes.UnsupportedType);
    }

    [Theory]
    [InlineData("ulong")]
    [InlineData("ulong?")]
    [InlineData("List<ulong>")]
    public void TooWideIntegerRanges_AreNotTruncated(string type)
    {
        AssertError(Analyze($"[ResourceType(\"R\")] public class Model {{ public {type} Value => default!; }}"),
            TypeGenDiagnosticCodes.UnsupportedIntegerRange);
    }

    [Theory]
    [InlineData("Dictionary<int, string>")]
    [InlineData("IDictionary<bool, string>")]
    [InlineData("IReadOnlyDictionary<System.Guid, string>")]
    [InlineData("System.Collections.Immutable.ImmutableDictionary<int, string>")]
    public void NonStringDictionaryKeys_AreNotTreatedAsEnumerablePairs(string type)
    {
        AssertError(Analyze($"[ResourceType(\"R\")] public class Model {{ public {type} Value => default!; }}"),
            TypeGenDiagnosticCodes.InvalidDictionaryKey);
    }

    [Fact]
    public void CustomUniqueEnumerable_IsSupportedButAmbiguousElementTypesAreRejected()
    {
        var valid = Analyze("""
            public class Strings : List<string?> { }
            [ResourceType("R")] public class Model { public Strings Value { get; set; } = new(); }
            """);
        Assert.IsType<StringTypeDefinition>(NullableValue(
            Assert.IsType<ArrayTypeDefinition>(Assert.Single(Body(valid).Properties).Type).ItemType));

        AssertError(Analyze("""
            public class Ambiguous : List<int>, IEnumerable<string>
            {
                IEnumerator<string> IEnumerable<string>.GetEnumerator() => throw new System.NotImplementedException();
            }
            [ResourceType("R")] public class Model { public Ambiguous Value { get; set; } = new(); }
            """), TypeGenDiagnosticCodes.AmbiguousCollection);
    }

    [Fact]
    public void ConflictingDictionaryInterfaces_AreNotResolvedByPickingTheFirst()
    {
        AssertError(Analyze("""
            public class Ambiguous : Dictionary<string, int>, IReadOnlyDictionary<string, string>
            {
                string IReadOnlyDictionary<string, string>.this[string key] => "";
                IEnumerable<string> IReadOnlyDictionary<string, string>.Keys => [];
                IEnumerable<string> IReadOnlyDictionary<string, string>.Values => [];
                bool IReadOnlyDictionary<string, string>.ContainsKey(string key) => false;
                bool IReadOnlyDictionary<string, string>.TryGetValue(string key, out string value) { value = ""; return false; }
                IEnumerator<KeyValuePair<string, string>> IEnumerable<KeyValuePair<string, string>>.GetEnumerator()
                    => throw new System.NotImplementedException();
            }
            [ResourceType("R")] public class Model { public Ambiguous Value { get; set; } = new(); }
            """), TypeGenDiagnosticCodes.AmbiguousCollection);
    }

    [Theory]
    [InlineData("string")]
    [InlineData("string?")]
    [InlineData("string[]")]
    [InlineData("List<string?>?")]
    [InlineData("List<List<string?>>")]
    public void SecureStringsAndCollectionElements_AreMarkedWithoutCachePollution(string type)
    {
        var result = Analyze($$"""
            [ResourceType("R")] public class Model
            {
                [TypeProperty(null, isSecure: true)] public {{type}} Secure { get; set; } = default!;
                public {{type}} Plain { get; set; } = default!;
            }
            """);
        var properties = Body(result).Properties;
        Assert.True(LeafString(properties[0].Type).Sensitive);
        Assert.False(LeafString(properties[1].Type).Sensitive);
        Assert.NotSame(properties[0].Type, properties[1].Type);
    }

    [Fact]
    public void SecureMetadata_DoesNotSpreadToMapValuesOrObjectChildren()
    {
        var result = Analyze("""
            public class Child { public string Plain { get; set; } = ""; [TypeProperty(null, isSecure: true)] public string Secret { get; set; } = ""; }
            [ResourceType("R")] public class Model
            {
                [TypeProperty(null, isSecure: true)] public Dictionary<string, string[]> Map { get; set; } = [];
                [TypeProperty(null, isSecure: true)] public Child Child { get; set; } = new();
                [TypeProperty(null, isSecure: true)] public List<Child> Children { get; set; } = [];
                [TypeProperty(null, isSecure: true)] public int Number { get; set; }
            }
            """);
        var properties = Body(result).Properties;
        var map = Assert.IsType<ObjectTypeDefinition>(properties[0].Type);
        Assert.False(map.Sensitive);
        Assert.False(LeafString(map.AdditionalProperties!).Sensitive);
        var child = Assert.IsType<ObjectTypeDefinition>(properties[1].Type);
        Assert.False(child.Sensitive);
        Assert.False(Assert.IsType<StringTypeDefinition>(child.Properties[0].Type).Sensitive);
        Assert.True(Assert.IsType<StringTypeDefinition>(child.Properties[1].Type).Sensitive);
        Assert.Same(child, Assert.IsType<ArrayTypeDefinition>(properties[2].Type).ItemType);
        Assert.IsType<IntegerTypeDefinition>(properties[3].Type);
    }

    [Fact]
    public void RepositoryAndConfiguration_ProduceCompleteBodiesWithVersionMetadataDefaults()
    {
        var result = Analyze("""
            public class Identifiers
            {
                [TypeProperty("Repository name", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
                public required string Name { get; set; }
            }
            [ResourceType("Repository", ApiVersions = new[] { "v2", "v1" })]
            public class Repository : Identifiers
            {
                [TypeProperty("Description")] public string? Description { get; set; }
                [TypeProperty("URL", ObjectTypePropertyFlags.ReadOnly)] public string Url { get; set; } = "";
            }
            [BicepConfiguration] public class Settings
            {
                [TypeProperty("Token", ObjectTypePropertyFlags.Required, true)] public string Token { get; set; } = "";
            }
            """);
        var model = Assert.IsType<ExtensionTypeSemanticModel>(result.Model);
        var body = Body(result);
        var resource = Assert.Single(model.ResourceTypes);
        Assert.Equal<string>(["v2", "v1"], resource.ApiVersions);
        Assert.Equal(["description", "url", "name"], body.Properties.Select(property => property.Name));
        Assert.IsType<StringTypeDefinition>(NullableValue(body.Properties[0].Type));
        Assert.Equal(PropertyFlags.ReadOnly, body.Properties[1].Flags);
        Assert.Equal(PropertyFlags.Identifier | PropertyFlags.Required, body.Properties[2].Flags);
        Assert.Equal("Repository name", body.Properties[2].Description);
        var configuration = Assert.IsType<ObjectTypeDefinition>(model.Configuration);
        var token = Assert.Single(configuration.Properties);
        Assert.True(Assert.IsType<StringTypeDefinition>(token.Type).Sensitive);
        Assert.Equal(PropertyFlags.Required, token.Flags);
        Assert.All(body.Properties, property => Assert.Equal(new PropertyVersioning(), property.Versioning));
    }

    [Fact]
    public void ObliviousReferences_KeepLegacyShapeWithExplicitSourceLocatedWarnings()
    {
        var result = Analyze("""
            #nullable disable
            [ResourceType("R")] public class Model
            {
                public string Name { get; set; }
                public List<string> Names { get; set; }
            }
            """);
        var body = Body(result);
        Assert.IsType<StringTypeDefinition>(body.Properties[0].Type);
        Assert.IsType<StringTypeDefinition>(Assert.IsType<ArrayTypeDefinition>(body.Properties[1].Type).ItemType);
        Assert.Equal(3, result.Diagnostics.Length);
        Assert.All(result.Diagnostics, diagnostic =>
        {
            Assert.Equal(TypeGenDiagnosticCodes.ObliviousNullability, diagnostic.Code);
            Assert.Equal(TypeGenDiagnosticSeverity.Warning, diagnostic.Severity);
            Assert.Equal("TypeFixture.cs", diagnostic.SourceLocation!.FilePath);
        });
    }

    [Fact]
    public void ExplicitNullableAndNonNullable_DoNotProduceObliviousWarnings()
    {
        var result = Analyze("""
            [ResourceType("R")] public class Model
            {
                public string Name { get; set; } = "";
                public string? Optional { get; set; }
            }
            """);
        Body(result);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void ReferencedSchema_IsTraversedOnlyWhenReachableAndPreservesNullableMetadata()
    {
        var library = RoslynCompilationFactory.Create(Imports + """
            [ResourceType("NotRegistered")] public class Child { public List<string?>? Values { get; set; } }
            [ResourceType("Unreachable")] public class Unreachable { public float Unsupported { get; set; } }
            """).WithAssemblyName("SchemaLibrary");
        var compilation = RoslynCompilationFactory.Create(Imports + """
            [ResourceType("R")] public class Model { public Child Child { get; set; } = new(); }
            """, "Current.cs").AddReferences(RoslynCompilationFactory.EmitReference(library));
        var result = new TypeGenerationFrontend().Build(compilation, new("E", "1"));
        var child = Assert.IsType<ObjectTypeDefinition>(Assert.Single(Body(result).Properties).Type);
        var values = Assert.IsType<ArrayTypeDefinition>(NullableValue(Assert.Single(child.Properties).Type));
        Assert.IsType<StringTypeDefinition>(NullableValue(values.ItemType));
        Assert.Equal("R", Assert.Single(result.Model!.ResourceTypes).Name);
        Assert.Equal("Current.cs", Assert.Single(child.Properties).SourceLocation!.FilePath);
    }

    [Fact]
    public void SelfCycleAndCollectionBackEdges_AreWiredWithoutTruncatingFields()
    {
        var result = Analyze("""
            [ResourceType("R")] public class Model
            {
                public Model? Parent { get; set; }
                public List<Model> Children { get; set; } = [];
                public Dictionary<string, Model?> Named { get; set; } = [];
                public int Number { get; set; }
            }
            """);
        var body = Body(result);
        Assert.Equal(4, body.Properties.Length);
        Assert.Same(body, NullableValue(body.Properties[0].Type));
        Assert.Same(body, Assert.IsType<ArrayTypeDefinition>(body.Properties[1].Type).ItemType);
        Assert.Same(body, NullableValue(Assert.IsType<ObjectTypeDefinition>(body.Properties[2].Type).AdditionalProperties!));
        Assert.IsType<IntegerTypeDefinition>(body.Properties[3].Type);
    }

    [Fact]
    public void MutualCycles_DoNotRecurseForever()
    {
        var body = Body(Analyze("""
            public class Child { public Model Owner { get; set; } = null!; public string Name { get; set; } = ""; }
            [ResourceType("R")] public class Model { public Child Child { get; set; } = new(); }
            """));
        var child = Assert.IsType<ObjectTypeDefinition>(Assert.Single(body.Properties).Type);
        Assert.Same(body, child.Properties[0].Type);
        Assert.IsType<StringTypeDefinition>(child.Properties[1].Type);
    }

    [Fact]
    public void ExpandingGenericRecursion_ReportsABoundedErrorInsteadOfOverflow()
    {
        AssertError(Analyze("""
            public class Growing<T> { public Growing<List<T>> Next { get; set; } = null!; }
            [ResourceType("R")] public class Model { public Growing<int> Value { get; set; } = new(); }
            """), TypeGenDiagnosticCodes.TypeGraphTooDeep);
    }

    [Theory]
    [InlineData(128, true)]
    [InlineData(129, false)]
    public void TypeExpansionDepth_EnforcesTheExactApprovedBoundary(int count, bool succeeds)
    {
        var declarations = Enumerable.Range(0, count).Select(index =>
            $"{(index == 0 ? "[ResourceType(\"R\")]" : "")} public class Node{index} {{ " +
            (index + 1 < count ? $"public Node{index + 1} Next => null!;" : "") + " }");
        var result = Analyze(string.Join(Environment.NewLine, declarations));
        if (!succeeds)
        {
            AssertError(result, TypeGenDiagnosticCodes.TypeGraphTooDeep);
            return;
        }

        var node = Body(result);
        for (var index = 1; index < count; index++)
        {
            node = Assert.IsType<ObjectTypeDefinition>(Assert.Single(node.Properties).Type);
        }
        Assert.Empty(node.Properties);
    }

    [Fact]
    public void EmptyEnum_ReportsAnErrorInsteadOfASuccessfulEmptyUnion()
    {
        AssertError(Analyze("""
            public enum Empty { }
            [ResourceType("R")] public class Model { public Empty Value { get; set; } }
            """), TypeGenDiagnosticCodes.UnsupportedType);
    }

    [Fact]
    public void LookalikeCollectionNames_DoNotOverrideSemanticIdentity()
    {
        var result = Analyze("""
            namespace Foreign
            {
                public class List<T> { public T Item { get; set; } = default!; }
                public class Dictionary<TKey, TValue> { public TValue Value { get; set; } = default!; }
            }
            [ResourceType("R")] public class Model
            {
                public Foreign.List<string> FakeList { get; set; } = new();
                public Foreign.Dictionary<int, bool> FakeMap { get; set; } = new();
                public List<string> RealList { get; set; } = [];
            }
            """);
        var properties = Body(result).Properties;
        Assert.IsType<StringTypeDefinition>(Assert.Single(Assert.IsType<ObjectTypeDefinition>(properties[0].Type).Properties).Type);
        Assert.IsType<BooleanTypeDefinition>(Assert.Single(Assert.IsType<ObjectTypeDefinition>(properties[1].Type).Properties).Type);
        Assert.IsType<ArrayTypeDefinition>(properties[2].Type);
    }

    [Fact]
    public void NullableStructEnumerable_PreservesBothOuterAndElementNullability()
    {
        var array = Assert.IsType<ArrayTypeDefinition>(NullableValue(
            Property("System.Collections.Immutable.ImmutableArray<string?>?")));
        Assert.IsType<StringTypeDefinition>(NullableValue(array.ItemType));
    }

    [Fact]
    public void NullableObliviousMetadata_HasAnExplicitWarningAtTheReachableUseSite()
    {
        var library = RoslynCompilationFactory.Create("""
            #nullable disable
            public class LegacyChild { public string Name { get; set; } }
            """).WithAssemblyName("LegacySchema");
        var compilation = RoslynCompilationFactory.Create(Imports + """
            [ResourceType("R")] public class Model { public LegacyChild Child { get; set; } = new(); }
            """, "LegacyUse.cs").AddReferences(RoslynCompilationFactory.EmitReference(library));
        var result = new TypeGenerationFrontend().Build(compilation, new("E", "1"));
        var child = Assert.IsType<ObjectTypeDefinition>(Assert.Single(Body(result).Properties).Type);
        Assert.IsType<StringTypeDefinition>(Assert.Single(child.Properties).Type);
        var warning = Assert.Single(result.Diagnostics);
        Assert.Equal(TypeGenDiagnosticCodes.ObliviousNullability, warning.Code);
        Assert.Equal(TypeGenDiagnosticSeverity.Warning, warning.Severity);
        Assert.Equal("LegacyUse.cs", warning.SourceLocation!.FilePath);
    }

    [Fact]
    public void CollectionRoot_IsRejectedRatherThanBeingUsedAsAResourceObject()
    {
        AssertError(Analyze("[ResourceType(\"R\")] public class Model : List<int> { }"),
            TypeGenDiagnosticCodes.UnsupportedSchema);
    }

    [Fact]
    public void ResourceAndConfigurationRoots_ReuseTheSameCompleteObject()
    {
        var result = Analyze("""
            [ResourceType("R"), BicepConfiguration] public class Model
            {
                public string Value { get; set; } = "";
            }
            """);
        var body = Body(result);
        Assert.Same(body, result.Model!.Configuration);
        Assert.IsType<StringTypeDefinition>(Assert.Single(body.Properties).Type);
    }

    [Fact]
    public void MultipleTypeErrors_AreReportedTogetherAndNeverExposePartialModel()
    {
        var result = Analyze("""
            [ResourceType("R")] public class Model
            {
                public float Unsupported { get; set; }
                public ulong TooWide { get; set; }
                public Dictionary<int, string> WrongKey { get; set; } = [];
            }
            """);
        Assert.False(result.Succeeded);
        Assert.Null(result.Model);
        Assert.Equal([TypeGenDiagnosticCodes.UnsupportedType, TypeGenDiagnosticCodes.UnsupportedIntegerRange,
            TypeGenDiagnosticCodes.InvalidDictionaryKey], result.Diagnostics.Select(diagnostic => diagnostic.Code));
        Assert.All(result.Diagnostics, diagnostic => Assert.NotNull(diagnostic.SourceLocation));
    }

    [Fact]
    public void UnsafePointerAndFunctionPointer_AreSemanticErrorsWhenCompilationIsValid()
    {
        var compilation = RoslynCompilationFactory.Create(Imports + """
            [ResourceType("R")] public unsafe class Model
            {
                public int* Pointer => null;
                public delegate*<int, int> Function => null;
            }
            """);
        compilation = compilation.WithOptions(compilation.Options.WithAllowUnsafe(true));
        var result = new TypeGenerationFrontend().Build(compilation, new("E", "1"));
        AssertError(result, TypeGenDiagnosticCodes.UnsupportedType);
        Assert.Equal(2, result.Diagnostics.Length);
    }

    [Fact]
    public void UnresolvedType_RemainsACompilerError()
    {
        AssertError(Analyze("[ResourceType(\"R\")] public class Model { public MissingType Value => default!; }"),
            TypeGenDiagnosticCodes.CSharpCompilation);
    }

    private static TypeDefinition Property(string type)
        => Assert.Single(Body(Analyze($"[ResourceType(\"R\")] public class Model {{ public {type} Value => default!; }}")).Properties).Type;

    private static TypeGenerationResult Analyze(string source)
        => new TypeGenerationFrontend().Build(
            RoslynCompilationFactory.Create(Imports + source, "TypeFixture.cs"), new("Contoso", "1.0.0"));

    private static ObjectTypeDefinition Body(TypeGenerationResult result)
    {
        Assert.True(result.Succeeded, string.Join(Environment.NewLine, result.Diagnostics));
        return Assert.IsType<ObjectTypeDefinition>(Assert.Single(result.Model!.ResourceTypes).Body);
    }

    private static TypeDefinition NullableValue(TypeDefinition type)
    {
        var union = Assert.IsType<UnionTypeDefinition>(type);
        Assert.Equal(2, union.Elements.Length);
        Assert.IsType<NullTypeDefinition>(union.Elements[1].Type);
        return union.Elements[0].Type;
    }

    private static StringTypeDefinition LeafString(TypeDefinition type)
        => type switch
        {
            StringTypeDefinition text => text,
            ArrayTypeDefinition array => LeafString(array.ItemType),
            UnionTypeDefinition => LeafString(NullableValue(type)),
            _ => throw new InvalidOperationException($"Unexpected test node: {type.GetType().Name}."),
        };

    private static void AssertError(TypeGenerationResult result, string code)
    {
        Assert.False(result.Succeeded);
        Assert.Null(result.Model);
        Assert.NotEmpty(result.Diagnostics);
        Assert.All(result.Diagnostics, diagnostic =>
        {
            Assert.Equal(TypeGenDiagnosticSeverity.Error, diagnostic.Severity);
            Assert.Equal(code, diagnostic.Code);
            Assert.NotNull(diagnostic.SourceLocation);
        });
    }
}
