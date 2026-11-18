// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using Azure.Bicep.Types;
using Azure.Bicep.Types.Concrete;
using Azure.Deployments.Extensibility.TypeGeneration.Backend.Lowering;
using Azure.Deployments.Extensibility.TypeGeneration.Backend.Projection;
using Azure.Deployments.Extensibility.TypeGeneration.Diagnostics;
using Azure.Deployments.Extensibility.TypeGeneration.Roslyn;
using Azure.Deployments.Extensibility.TypeGeneration.Semantics;
using Azure.Deployments.Extensibility.TypeGeneration.Tests.Support;
using Xunit;

namespace Azure.Deployments.Extensibility.TypeGeneration.Tests.Unit.Backend;

[Trait("Category", "Unit")]
public class TypeGenerationLowererTests
{
    [Fact]
    public void ScalarsAndConstraints_MapToDedicatedTargetKinds()
    {
        var source = Object("Scalars",
            Property("any", new AnyTypeDefinition()),
            Property("null", new NullTypeDefinition()),
            Property("bool", new BooleanTypeDefinition()),
            Property("integer", new IntegerTypeDefinition { MinValue = long.MinValue, MaxValue = long.MaxValue }),
            Property("unbounded", new IntegerTypeDefinition()),
            Property("string", new StringTypeDefinition { Sensitive = true, MinLength = 0, MaxLength = 20, Pattern = "[" }),
            Property("plain", new StringTypeDefinition()),
            Property("literal", new StringLiteralTypeDefinition { Value = "" }));
        var result = Lower(Project(Model(Resource("Repository", [], source))));
        var body = ResourceBody(result, "Repository");
        Assert.IsType<AnyType>(body.Properties["any"].Type.Type);
        Assert.IsType<NullType>(body.Properties["null"].Type.Type);
        Assert.IsType<BooleanType>(body.Properties["bool"].Type.Type);
        var integer = Assert.IsType<IntegerType>(body.Properties["integer"].Type.Type);
        Assert.Equal(long.MinValue, integer.MinValue);
        Assert.Equal(long.MaxValue, integer.MaxValue);
        Assert.Null(Assert.IsType<IntegerType>(body.Properties["unbounded"].Type.Type).MinValue);
        Assert.Null(Assert.IsType<IntegerType>(body.Properties["unbounded"].Type.Type).MaxValue);
        var text = Assert.IsType<StringType>(body.Properties["string"].Type.Type);
        Assert.True(text.Sensitive);
        Assert.Equal(0, text.MinLength);
        Assert.Equal(20, text.MaxLength);
        Assert.Equal("[", text.Pattern);
        var plain = Assert.IsType<StringType>(body.Properties["plain"].Type.Type);
        Assert.Null(plain.Sensitive);
        Assert.Null(plain.MinLength);
        Assert.Null(plain.MaxLength);
        Assert.Null(plain.Pattern);
        Assert.Equal("", Assert.IsType<StringLiteralType>(body.Properties["literal"].Type.Type).Value);
        Assert.DoesNotContain(result.Types, type => type is BuiltInType);
    }

    [Fact]
    public void NestedNullableArraysMapsAndEnums_PreserveEdgesAndOrder()
    {
        var text = new StringTypeDefinition();
        var nullableText = Union(text, new NullTypeDefinition());
        var array = new ArrayTypeDefinition { ItemType = nullableText, MinLength = 0, MaxLength = 7 };
        var enumType = Union(new StringLiteralTypeDefinition { Value = "Zulu" }, new StringLiteralTypeDefinition { Value = "Alpha" });
        var map = Object("Map") with { AdditionalProperties = nullableText };
        var body = Object("Body", Property("array", Union(array, new NullTypeDefinition())), Property("enum", enumType), Property("map", map));
        var result = Lower(Project(Model(Resource("R", [], body))));
        var actual = ResourceBody(result, "R");
        var outer = Assert.IsType<UnionType>(actual.Properties["array"].Type.Type);
        var actualArray = Assert.IsType<ArrayType>(outer.Elements[0].Type);
        Assert.IsType<NullType>(outer.Elements[1].Type);
        Assert.Equal(0, actualArray.MinLength);
        Assert.Equal(7, actualArray.MaxLength);
        var item = Assert.IsType<UnionType>(actualArray.ItemType.Type);
        Assert.IsType<StringType>(item.Elements[0].Type);
        Assert.IsType<NullType>(item.Elements[1].Type);
        var actualMap = Assert.IsType<ObjectType>(actual.Properties["map"].Type.Type);
        Assert.Empty(actualMap.Properties);
        Assert.Same(item, actualMap.AdditionalProperties!.Type);
        var actualEnum = Assert.IsType<UnionType>(actual.Properties["enum"].Type.Type);
        Assert.Equal<string>(["Zulu", "Alpha"], actualEnum.Elements.Select(element => Assert.IsType<StringLiteralType>(element.Type).Value));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ObjectMetadata_AndRequiredDefaultValues_ArePreserved(bool sensitive)
    {
        var source = Object("Named",
            Property("identifier", new StringTypeDefinition()) with { Flags = PropertyFlags.Required | PropertyFlags.Identifier, Description = "Identity" },
            Property("none", new BooleanTypeDefinition()),
            Property("empty", new StringTypeDefinition()) with { Description = "" }) with { Sensitive = sensitive };
        var result = Lower(Project(Model(Resource("R", ["v1"], source))));
        var body = ResourceBody(result, "R@v1");
        Assert.Equal("Named", body.Name);
        bool? expectedSensitive = sensitive ? true : null;
        Assert.Equal(expectedSensitive, body.Sensitive);
        Assert.Equal(17, (int)body.Properties["identifier"].Flags);
        Assert.Equal("Identity", body.Properties["identifier"].Description);
        Assert.Equal(0, (int)body.Properties["none"].Flags);
        Assert.Null(body.Properties["none"].Description);
        Assert.Equal("", body.Properties["empty"].Description);
        Assert.Null(body.AdditionalProperties);
    }

    [Fact]
    public void EveryDefinedFlagAndScopeCombination_IsMappedExplicitly()
    {
        for (var bits = 0; bits <= 31; bits++)
        {
            var source = Object("Body", Property("value", new BooleanTypeDefinition()) with { Flags = (PropertyFlags)bits });
            var resource = Resource("R", [], source) with { ReadableScopes = (ResourceScope)bits, WritableScopes = (ResourceScope)(31 - bits) };
            var result = Lower(Project(Model(resource)));
            var actual = TargetResource(result, "R");
            Assert.Equal(bits, (int)actual.ReadableScopes);
            Assert.Equal(31 - bits, (int)actual.WritableScopes);
            Assert.Equal(bits, (int)ResourceBody(result, "R").Properties["value"].Flags);
            Assert.Equal(actual.ReadableScopes, actual.ReadableScopes_in);
            Assert.Equal(actual.WritableScopes, actual.WritableScopes_in);
            Assert.Null(actual.Functions);
        }
    }

    [Theory]
    [InlineData(null, "R")]
    [InlineData("v1", "R@v1")]
    [InlineData("  preview@label  ", "R@  preview@label  ")]
    public void ResourceNames_EncodeOnlyTheConcreteApiVersion(string? version, string expected)
    {
        var result = Lower(Handcrafted([Projected("R", version, Object("Body"))]));
        var entry = Assert.Single(result.Index.Resources);
        Assert.Equal(expected, entry.Key);
        Assert.Equal(expected, Assert.IsType<ResourceType>(result.Types[entry.Value.Index]).Name);
        Assert.Equal("1.2", result.Index.Settings!.Version);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void EmptyAndConfigurationOnlyModels_PreserveSettingsAndDoNotEmitLegacySettings(bool configuration, bool singleton)
    {
        var source = Model() with
        {
            Name = " Contoso ",
            Version = "not-semver",
            IsSingleton = singleton,
            Configuration = configuration ? Object("Configuration") : null,
        };
        var result = Lower(Project(source));
        Assert.Empty(result.Index.Resources);
        var settings = result.Index.Settings!;
        Assert.Equal(source.Name, settings.Name);
        Assert.Equal(source.Version, settings.Version);
        Assert.Equal(singleton, settings.IsSingleton);
        Assert.Null(settings.IsPreview);
        Assert.Null(settings.IsDeprecated);
        Assert.Null(result.Index.FallbackResourceType);
        Assert.Empty(result.Index.ResourceFunctions);
        Assert.Empty(result.Index.NamespaceFunctions);
        if (configuration)
        {
            Assert.Equal(0, settings.ConfigurationType!.Index);
            Assert.Equal("Configuration", Assert.IsType<ObjectType>(Assert.Single(result.Types)).Name);
        }
        else
        {
            Assert.Null(settings.ConfigurationType);
            Assert.Empty(result.Types);
        }
    }

    [Fact]
    public void ActualFrontendAndProjection_ProduceCompleteContosoTargetGraph()
    {
        var frontend = new TypeGenerationFrontend().Build(
            RoslynCompilationFactory.Create(ContosoSampleFixture.ReadSources()), new("Contoso", "1.2"));
        Assert.True(frontend.Succeeded);
        var result = Lower(Project(frontend.Model!));
        Assert.Equal<string>(["Repository@2026-08-01", "Repository@2026-09-01"], result.Index.Resources.Keys);
        var first = TargetResource(result, "Repository@2026-08-01");
        var second = TargetResource(result, "Repository@2026-09-01");
        Assert.NotSame(first, second);
        Assert.Same(first.Body.Type, second.Body.Type);
        Assert.Equal(31, (int)first.ReadableScopes);
        Assert.Equal(31, (int)first.WritableScopes);
        var body = Assert.IsType<ObjectType>(first.Body.Type);
        Assert.Equal(17, (int)body.Properties["name"].Flags);
        Assert.Equal(ObjectTypePropertyFlags.ReadOnly, body.Properties["url"].Flags);
        Assert.Contains(Assert.IsType<UnionType>(body.Properties["description"].Type.Type).Elements,
            element => element.Type is NullType);
        var settings = Assert.IsType<UnionType>(body.Properties["settings"].Type.Type);
        var settingsObject = Assert.IsType<ObjectType>(settings.Elements[0].Type);
        Assert.Same(settings, settingsObject.Properties["parent"].Type.Type);
        Assert.IsType<ArrayType>(settingsObject.Properties["topics"].Type.Type);
        Assert.NotNull(Assert.IsType<ObjectType>(settingsObject.Properties["labels"].Type.Type).AdditionalProperties);
        Assert.Equal("ContosoConfiguration",
            Assert.IsType<ObjectType>(result.Types[result.Index.Settings!.ConfigurationType!.Index]).Name);
    }

    [Fact]
    public void SelfMutualArrayUnionAndAdditionalPropertyCycles_AreClosed()
    {
        var body = new ObjectTypeDefinition("Node", self =>
        {
            var peer = new ObjectTypeDefinition("Peer", _ => ([Property("owner", self)], null));
            return ([
                Property("self", self),
                Property("peer", peer),
                Property("array", new ArrayTypeDefinition(array => array)),
                Property("union", new UnionTypeDefinition(union => [new() { Type = union }, new() { Type = new NullTypeDefinition() }])),
            ], self);
        });
        var result = Lower(Project(Model(Resource("R", ["v1", "v2"], body)) with { Configuration = body }));
        var node = ResourceBody(result, "R@v1");
        Assert.Same(node, result.Types[result.Index.Settings!.ConfigurationType!.Index]);
        Assert.Same(node, node.Properties["self"].Type.Type);
        Assert.Same(node, node.AdditionalProperties!.Type);
        Assert.Same(node, Assert.IsType<ObjectType>(node.Properties["peer"].Type.Type).Properties["owner"].Type.Type);
        var array = Assert.IsType<ArrayType>(node.Properties["array"].Type.Type);
        Assert.Same(array, array.ItemType.Type);
        var union = Assert.IsType<UnionType>(node.Properties["union"].Type.Type);
        Assert.Same(union, union.Elements[0].Type);
        Assert.Same(node, ResourceBody(result, "R@v2"));
    }

    [Fact]
    public void SameNamesAndEqualShapes_DoNotCauseStructuralDeduplication()
    {
        var shared = new StringTypeDefinition();
        var left = Object("Same", Property("a", shared), Property("b", new StringTypeDefinition()));
        var right = Object("Same", Property("a", shared), Property("b", new StringTypeDefinition()));
        var result = Lower(Project(Model(Resource("R", ["v1"], left), Resource("R", ["v2"], right))));
        var first = ResourceBody(result, "R@v1");
        var second = ResourceBody(result, "R@v2");
        Assert.Equal(first.Name, second.Name);
        Assert.NotSame(first, second);
        Assert.Same(first.Properties["a"].Type.Type, second.Properties["a"].Type.Type);
        Assert.NotSame(first.Properties["a"].Type.Type, first.Properties["b"].Type.Type);
        Assert.NotSame(first.Properties["b"].Type.Type, second.Properties["b"].Type.Type);
    }

    [Fact]
    public void RegistrationIsConfigurationFirstDepthFirstAndResourceKeyOrdinal()
    {
        var config = Object("Config", Property("value", new BooleanTypeDefinition()));
        var shared = new StringTypeDefinition();
        var body = Object("Body", Property("z", shared), Property("a", new ArrayTypeDefinition { ItemType = shared }));
        var result = Lower(Project(Model(Resource("z", [], body), Resource("A", [], body)) with { Configuration = config }));
        Assert.Equal<string>(["ObjectType", "BooleanType", "ObjectType", "ArrayType", "StringType", "ResourceType", "ResourceType"],
            result.Types.Select(type => type.GetType().Name));
        Assert.Equal(0, result.Index.Settings!.ConfigurationType!.Index);
        Assert.Equal<string>(["A", "z"], result.Index.Resources.Keys);
        Assert.Equal<string>(["a", "z"], ResourceBody(result, "A").Properties.Keys);
        Assert.Equal(5, result.Index.Resources["A"].Index);
        Assert.Equal(6, result.Index.Resources["z"].Index);
    }

    [Fact]
    public void EquivalentGraphsHaveStableIndicesDespiteInputOrderSourceLocationsAndNewInstances()
    {
        static ExtensionTypeSemanticModel Input(bool reverse)
        {
            var text = new StringTypeDefinition { SourceLocation = Location(reverse ? "z.cs" : "a.cs", reverse ? 9 : 1) };
            var body = Object("Body", Property("z", text), Property("a", new ArrayTypeDefinition { ItemType = text }));
            if (reverse)
            {
                body = body with { Properties = [.. body.Properties.Reverse()] };
            }
            var first = Resource("Z", ["v2", "v1"], body);
            var second = Resource("A", [], body);
            return Model(reverse ? [second, first] : [first, second]) with { Configuration = Object("Config", Property("value", text)) };
        }

        var first = Lower(Project(Input(false)));
        var second = Lower(Project(Input(true)));
        Assert.Equal<object?>(Signature(first), Signature(second));
    }

    [Fact]
    public void UnionOrderRemainsSemanticRatherThanAlphabetical()
    {
        var result = Lower(Project(Model(Resource("R", [], Object("Body", Property("value",
            Union(new StringLiteralTypeDefinition { Value = "z" }, new StringLiteralTypeDefinition { Value = "a" })))))));
        var union = Assert.IsType<UnionType>(ResourceBody(result, "R").Properties["value"].Type.Type);
        Assert.Equal<string>(["z", "a"], union.Elements.Select(element => Assert.IsType<StringLiteralType>(element.Type).Value));
    }

    [Fact]
    public void DeepGraphs_DoNotUseRecursiveLowering()
    {
        TypeDefinition type = new BooleanTypeDefinition();
        for (var depth = 0; depth < 4096; depth++)
        {
            type = new ArrayTypeDefinition { ItemType = type };
        }
        var result = Lower(Project(Model(Resource("R", [], Object("Body", Property("value", type))))));
        TypeBase current = ResourceBody(result, "R").Properties["value"].Type.Type;
        for (var depth = 0; depth < 4096; depth++)
        {
            current = Assert.IsType<ArrayType>(current).ItemType.Type;
        }
        Assert.IsType<BooleanType>(current);
        Assert.Equal(4099, result.Types.Length);
    }

    [Theory]
    [InlineData("unversioned-name")]
    [InlineData("delimiter")]
    [InlineData("duplicate")]
    public void EncodedKeyCollisions_FailWithBothSourcesAndNoPartialGraph(string scenario)
    {
        var body = Object("Body");
        var first = Projected(scenario == "delimiter" ? "R@a" : "R", scenario == "delimiter" ? "b" : "v1", body) with { SourceLocation = Location("a.cs", 1) };
        var second = scenario switch
        {
            "unversioned-name" => Projected("R@v1", null, body),
            "delimiter" => Projected("R", "a@b", body),
            _ => Projected("R", "v1", body),
        };
        second = second with { SourceLocation = Location("b.cs", 2) };
        var result = AssertFailure(Handcrafted([Projected("Valid", null, body), second, first]), "TG2201");
        Assert.Equal(2, result.Diagnostics.Length);
        Assert.Equal<SourceLocation?>([first.SourceLocation, second.SourceLocation], result.Diagnostics.Select(diagnostic => diagnostic.SourceLocation));
        Assert.Equal<TypeGenDiagnostic>(result.Diagnostics, TypeGenerationLowerer.Lower(Handcrafted([first, second])).Diagnostics);
        Assert.Equal("TG2201", TypeGenDiagnosticCodes.BackendResourceIdentityCollision);
    }

    [Theory]
    [InlineData("metadata", "TG2001")]
    [InlineData("name", "TG2002")]
    [InlineData("version", "TG2003")]
    [InlineData("mixed", "TG2004")]
    [InlineData("root", "TG2005")]
    [InlineData("null-edge", "TG2006")]
    [InlineData("duplicate-property", "TG2007")]
    [InlineData("flags", "TG2008")]
    [InlineData("scope", "TG2009")]
    [InlineData("constraint", "TG2010")]
    [InlineData("lifecycle", "TG2011")]
    [InlineData("union-lifecycle", "TG2011")]
    [InlineData("function", "TG2011")]
    [InlineData("functions-default", "TG2006")]
    [InlineData("function-null", "TG2006")]
    [InlineData("discriminated", "TG2012")]
    [InlineData("unknown", "TG2012")]
    public void HandcraftedProjectedInputs_CannotBypassValidation(string scenario, string code)
    {
        var body = Object("Body");
        var resource = Projected("R", "v1", body);
        var source = Model() with { Name = scenario == "metadata" ? "" : "Contoso" };
        TypeDefinition invalid = scenario switch
        {
            "null-edge" => new ArrayTypeDefinition { ItemType = null! },
            "constraint" => new IntegerTypeDefinition { MinValue = 2, MaxValue = 1 },
            "union-lifecycle" => new UnionTypeDefinition { Elements = [new() { Type = new BooleanTypeDefinition(), Availability = new() { AddedAt = "v1" } }] },
            "discriminated" => new DiscriminatedObjectTypeDefinition
            {
                Name = "Shape", Discriminator = "kind", BaseProperties = [], Cases = ImmutableDictionary<string, ObjectTypeDefinition>.Empty,
            },
            "unknown" => new PoisonType(),
            _ => new BooleanTypeDefinition(),
        };
        var property = Property("value", invalid);
        if (scenario == "flags") { property = property with { Flags = (PropertyFlags)32 }; }
        if (scenario == "lifecycle") { property = property with { Versioning = new() { NameChangedFrom = new() { AtVersion = "v1", PreviousValue = "old" } } }; }
        body = Object("Body", property);
        if (scenario == "duplicate-property") { body = body with { Properties = [property, property] }; }
        resource = resource with { Body = body };
        resource = scenario switch
        {
            "name" => resource with { Name = "" },
            "version" => resource with { ApiVersion = " " },
            "root" => resource with { Body = new StringTypeDefinition() },
            "scope" => resource with { WritableScopes = (ResourceScope)32 },
            "function" => resource with { ResourceFunctions = [new() { Name = "run", OutputType = new BooleanTypeDefinition() }] },
            "functions-default" => resource with { ResourceFunctions = default },
            "function-null" => resource with { ResourceFunctions = [null!] },
            _ => resource,
        };
        ImmutableArray<ProjectedResourceTypeDefinition> resources = scenario == "mixed"
            ? [resource, resource with { ApiVersion = null }]
            : [Projected("Valid", null, Object("Valid")), resource];
        AssertFailure(new(source, resources), code);
    }

    [Fact]
    public void InvalidConfiguration_PreservesTheMostSpecificDiagnosticLocation()
    {
        var location = Location("config.cs", 15);
        var config = Object("Config", Property("value", new IntegerTypeDefinition { MinValue = 5, MaxValue = 2 }) with { SourceLocation = location });
        var result = AssertFailure(new(Model() with { Configuration = config }, []), "TG2010");
        Assert.Same(location, Assert.Single(result.Diagnostics).SourceLocation);
    }

    [Fact]
    public async Task RepeatedAndConcurrentCalls_DoNotShareTargetObjectsOrMutateInput()
    {
        var source = ContosoSampleFixture.CreateExpected();
        var projected = Project(source);
        var first = Lower(projected);
        var results = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => Task.Run(() => Lower(projected))));
        Assert.All(results, result =>
        {
            Assert.NotSame(first, result);
            Assert.NotSame(first.Types[0], result.Types[0]);
            Assert.Equal<object?>(Signature(first), Signature(result));
        });
        Assert.True(SemanticModelComparer.AreEqual(ContosoSampleFixture.CreateExpected(), source));
        Assert.Same(source.ResourceTypes[0].Body, projected.ResourceTypes[0].Body);
    }

    [Fact]
    public void NullAndCancellation_AreExceptionsRatherThanFailureModels()
    {
        Assert.Equal("model", Assert.Throws<ArgumentNullException>(() => TypeGenerationLowerer.Lower(null!)).ParamName);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var error = Assert.Throws<OperationCanceledException>(() => TypeGenerationLowerer.Lower(Project(Model()), cancellation.Token));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Empty(Lower(Project(Model())).Types);
    }

    [Fact]
    public void ResultContracts_RejectMissingContentAndPartialSuccess()
    {
        var model = Lower(Project(Model()));
        Assert.Throws<ArgumentException>(() => new TypeGenerationLoweringResult(null, default));
        Assert.Throws<ArgumentException>(() => new TypeGenerationLoweringResult(null, [null!]));
        Assert.Throws<ArgumentException>(() => new TypeGenerationLoweringResult(model,
            [new("TG2201", TypeGenDiagnosticSeverity.Error, "Collision.", null)]));
        Assert.False(new TypeGenerationLoweringResult(null, []).Succeeded);
        Assert.True(new TypeGenerationLoweringResult(model,
            [new("TG2201", TypeGenDiagnosticSeverity.Warning, "Example.", null)]).Succeeded);
        Assert.Throws<ArgumentNullException>(() => new TypeGenerationLoweredPackage(null!, []));
        Assert.Throws<ArgumentException>(() => new TypeGenerationLoweredPackage(model.Index, default));
        Assert.Throws<ArgumentException>(() => new TypeGenerationLoweredPackage(model.Index, [null!]));
        Assert.False(typeof(TypeGenerationLowerer).IsPublic);
        Assert.False(typeof(TypeGenerationLoweredPackage).IsPublic);
    }

    private static TypeGenerationLoweredPackage Lower(TypeGenerationProjectedExtension model)
    {
        var result = TypeGenerationLowerer.Lower(model);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        var lowered = Assert.IsType<TypeGenerationLoweredPackage>(result.Model);
        AssertClosed(lowered);
        return lowered;
    }

    private static void AssertClosed(TypeGenerationLoweredPackage model)
    {
        var nodes = new HashSet<TypeBase>(model.Types, ReferenceEqualityComparer.Instance);
        Assert.Equal(model.Types.Length, nodes.Count);
        foreach (var type in model.Types)
        {
            foreach (var reference in References(type))
            {
                Assert.Contains(nodes, node => ReferenceEquals(node, reference.Type));
            }
        }
        foreach (var reference in model.Index.Resources.Values)
        {
            Assert.Equal(TypeGenerationLoweredPackage.TypesFileName, reference.RelativePath);
            Assert.InRange(reference.Index, 0, model.Types.Length - 1);
            Assert.IsType<ResourceType>(model.Types[reference.Index]);
        }
        if (model.Index.Settings!.ConfigurationType is { } configuration)
        {
            Assert.Equal(TypeGenerationLoweredPackage.TypesFileName, configuration.RelativePath);
            Assert.InRange(configuration.Index, 0, model.Types.Length - 1);
            Assert.IsType<ObjectType>(model.Types[configuration.Index]);
        }
    }

    private static IEnumerable<ITypeReference> References(TypeBase type) => type switch
    {
        ArrayType array => [array.ItemType],
        ObjectType obj => obj.Properties.Values.Select(property => property.Type)
            .Concat(obj.AdditionalProperties is { } additional ? [additional] : []),
        UnionType union => union.Elements,
        ResourceType resource => [resource.Body],
        _ => [],
    };

    private static object?[] Signature(TypeGenerationLoweredPackage model)
    {
        var indices = new Dictionary<TypeBase, int>(ReferenceEqualityComparer.Instance);
        for (var index = 0; index < model.Types.Length; index++) { indices.Add(model.Types[index], index); }
        List<object?> signature = [model.Index.Settings!.Name, model.Index.Settings.Version,
            model.Index.Settings.IsSingleton, model.Index.Settings.ConfigurationType?.Index];
        foreach (var entry in model.Index.Resources) { signature.AddRange([entry.Key, entry.Value.RelativePath, entry.Value.Index]); }
        foreach (var type in model.Types)
        {
            signature.Add(type.GetType().Name);
            switch (type)
            {
                case IntegerType value:
                    signature.AddRange([value.MinValue, value.MaxValue]);
                    break;
                case StringType value:
                    signature.AddRange([value.Sensitive, value.MinLength, value.MaxLength, value.Pattern]);
                    break;
                case StringLiteralType value:
                    signature.Add(value.Value);
                    break;
                case ArrayType value:
                    signature.AddRange([value.MinLength, value.MaxLength, indices[value.ItemType.Type]]);
                    break;
                case ObjectType value:
                    signature.AddRange([value.Name, value.Sensitive, value.AdditionalProperties is { } additional ? indices[additional.Type] : null]);
                    foreach (var property in value.Properties)
                    {
                        signature.AddRange([property.Key, property.Value.Flags, property.Value.Description, indices[property.Value.Type.Type]]);
                    }
                    break;
                case ResourceType value:
                    signature.AddRange([value.Name, value.ReadableScopes, value.WritableScopes, indices[value.Body.Type]]);
                    break;
                case UnionType value:
                    signature.AddRange(value.Elements.Select(element => (object?)indices[element.Type]));
                    break;
            }
        }
        return [.. signature];
    }

    private static TypeGenerationLoweringResult AssertFailure(TypeGenerationProjectedExtension model, string code)
    {
        var result = TypeGenerationLowerer.Lower(model);
        Assert.False(result.Succeeded);
        Assert.Null(result.Model);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == code && diagnostic.Severity == TypeGenDiagnosticSeverity.Error);
        return result;
    }

    private static ResourceType TargetResource(TypeGenerationLoweredPackage model, string key) =>
        Assert.IsType<ResourceType>(model.Types[model.Index.Resources[key].Index]);

    private static ObjectType ResourceBody(TypeGenerationLoweredPackage model, string key) =>
        Assert.IsType<ObjectType>(TargetResource(model, key).Body.Type);

    private static TypeGenerationProjectedExtension Project(ExtensionTypeSemanticModel model)
    {
        var result = TypeGenerationProjectionService.Project(model);
        Assert.True(result.Succeeded);
        return Assert.IsType<TypeGenerationProjectedExtension>(result.Model);
    }

    private static TypeGenerationProjectedExtension Handcrafted(ImmutableArray<ProjectedResourceTypeDefinition> resources) => new(Model(), resources);

    private static ExtensionTypeSemanticModel Model(params ResourceTypeDefinition[] resources) =>
        new() { Name = "Contoso", Version = "1.2", ResourceTypes = [.. resources] };

    private static ResourceTypeDefinition Resource(string name, ImmutableArray<string> versions, ObjectTypeDefinition body) =>
        new() { Name = name, ApiVersions = versions, Body = body, ReadableScopes = ResourceScope.All, WritableScopes = ResourceScope.All, ResourceFunctions = [] };

    private static ProjectedResourceTypeDefinition Projected(string name, string? version, TypeDefinition body) =>
        new() { Name = name, ApiVersion = version, Body = body, ReadableScopes = ResourceScope.All, WritableScopes = ResourceScope.All, ResourceFunctions = [] };

    private static ObjectTypeDefinition Object(string name, params PropertyDefinition[] properties) =>
        new() { Name = name, Properties = [.. properties] };

    private static PropertyDefinition Property(string name, TypeDefinition type) => new() { Name = name, Type = type };

    private static UnionTypeDefinition Union(params TypeDefinition[] types) =>
        new() { Elements = [.. types.Select(type => new UnionElementDefinition { Type = type })] };

    private static SourceLocation Location(string path, int start) => new() { FilePath = path, Start = start, Length = 1 };

    private sealed record PoisonType : TypeDefinition
    {
        public override int GetHashCode() => throw new InvalidOperationException("Structural hashing must not be used.");

        public override string ToString() => throw new InvalidOperationException("Structural display must not be used.");
    }
}
