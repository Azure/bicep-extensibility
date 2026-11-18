// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using Azure.Deployments.Extensibility.TypeGeneration.Backend.Validation;
using Azure.Deployments.Extensibility.TypeGeneration.Diagnostics;
using Azure.Deployments.Extensibility.TypeGeneration.Roslyn;
using Azure.Deployments.Extensibility.TypeGeneration.Semantics;
using Azure.Deployments.Extensibility.TypeGeneration.Tests.Support;
using Xunit;

namespace Azure.Deployments.Extensibility.TypeGeneration.Tests.Unit.Backend;

[Trait("Category", "Unit")]
public class TypeGenerationModelValidatorTests
{
    [Fact]
    public void ExistingFixturesAndActualFrontendOutput_AreAcceptedWithoutMutation()
    {
        var frontend = new TypeGenerationFrontend().Build(
            RoslynCompilationFactory.Create(ContosoAuthoringFixture.Sources.ToArray()), new("Contoso", "1.2"));
        Assert.True(frontend.Succeeded);
        foreach (var model in new[] { SemanticModelFixtures.CreateContoso(), ContosoSampleFixture.CreateExpected(), frontend.Model! })
        {
            var resources = model.ResourceTypes;
            var configuration = model.Configuration;
            Assert.Empty(TypeGenerationModelValidator.Validate(model));
            Assert.Equal(resources, model.ResourceTypes);
            Assert.Same(configuration, model.Configuration);
        }
    }

    [Fact]
    public void EmptyAndConfigurationOnlyModels_AreValid()
    {
        Assert.Empty(TypeGenerationModelValidator.Validate(Model() with { ResourceTypes = [] }));
        Assert.Empty(TypeGenerationModelValidator.Validate(Model() with { ResourceTypes = [], Configuration = Object() }));
        Assert.Empty(TypeGenerationModelValidator.Validate(Model()));
    }

    [Fact]
    public void AllSupportedKindsAndConstraints_AreAcceptedWithoutAuthoringRestrictions()
    {
        TypeDefinition[] types =
        [
            new AnyTypeDefinition(), new NullTypeDefinition(), new BooleanTypeDefinition(),
            new IntegerTypeDefinition(), new IntegerTypeDefinition { MinValue = long.MinValue, MaxValue = long.MaxValue },
            new IntegerTypeDefinition { MaxValue = 0 },
            new StringTypeDefinition { Sensitive = true, MinLength = 0, MaxLength = long.MaxValue, Pattern = "[" },
            new StringLiteralTypeDefinition { Value = "" },
            new ArrayTypeDefinition { ItemType = new NullTypeDefinition(), MinLength = 0, MaxLength = 0 },
            new UnionTypeDefinition { Elements = [new() { Type = new StringLiteralTypeDefinition { Value = "" } }] },
            Object(new PropertyDefinition { Name = "known", Type = new BooleanTypeDefinition() }) with
            {
                Sensitive = true,
                AdditionalProperties = new AnyTypeDefinition(),
            },
        ];
        foreach (var type in types)
        {
            Assert.Empty(TypeGenerationModelValidator.Validate(WithType(type)));
        }
    }

    [Fact]
    public void FunctionIndexNames_AreNotBannedAsOrdinaryBusinessProperties()
    {
        var body = Object(
            new() { Name = "resourceFunctions", Type = new StringTypeDefinition() },
            new() { Name = "namespaceFunctions", Type = new ArrayTypeDefinition { ItemType = new StringTypeDefinition() } });
        Assert.Empty(TypeGenerationModelValidator.Validate(Model(body)));
    }

    [Fact]
    public void DisjointVersionsAndOrdinalLabels_AreAcceptedAndPreserved()
    {
        var first = Resource("Repository", ["v2", "v1"]);
        var second = Resource("Repository", ["V1", "  preview  "]);
        var model = Model() with
        {
            Name = " Contoso ",
            Version = "not-semver",
            ResourceTypes = [second, first, Resource("repository", [])],
        };
        Assert.Empty(TypeGenerationModelValidator.Validate(model));
        Assert.Same(second, model.ResourceTypes[0]);
        Assert.Equal<string>(["v2", "v1"], first.ApiVersions);
        Assert.Equal<string>(["V1", "  preview  "], second.ApiVersions);
        Assert.Equal(" Contoso ", model.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t")]
    public void MissingMetadata_IsDiagnosed(string? value)
    {
        var diagnostics = TypeGenerationModelValidator.Validate(Model() with { Name = value!, Version = value! });
        Assert.Equal(2, diagnostics.Length);
        Assert.All(diagnostics, diagnostic => Assert.Equal("TG2001", diagnostic.Code));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t")]
    public void InvalidResourceName_IsDiagnosed(string? value)
    {
        AssertCode(Model() with { ResourceTypes = [Resource(value!, [])] }, "TG2002");
    }

    [Theory]
    [InlineData("default")]
    [InlineData("null")]
    [InlineData("blank")]
    [InlineData("duplicate")]
    public void InvalidVersions_AreDiagnosed(string scenario)
    {
        ImmutableArray<string> versions = scenario switch
        {
            "default" => default,
            "null" => [null!],
            "blank" => [" "],
            _ => ["v1", "v1"],
        };
        AssertCode(Model() with { ResourceTypes = [Resource("Repository", versions)] }, "TG2003");
    }

    [Theory]
    [InlineData("overlap")]
    [InlineData("unversioned")]
    [InlineData("mixed")]
    public void RegistrationConflicts_ReportBothSourcesRegardlessOfInputOrder(string scenario)
    {
        var first = Resource("Repository", scenario == "unversioned" ? [] : ["v1"]) with { SourceLocation = Location("a.cs", 1) };
        var second = Resource("Repository", scenario == "overlap" ? ["v1", "v2"] : []) with { SourceLocation = Location("b.cs", 2) };
        var diagnostics = TypeGenerationModelValidator.Validate(Model() with { ResourceTypes = [second, first] });
        Assert.Equal(2, diagnostics.Length);
        Assert.All(diagnostics, diagnostic => Assert.Equal("TG2004", diagnostic.Code));
        Assert.Equal([first.SourceLocation, second.SourceLocation], diagnostics.Select(diagnostic => diagnostic.SourceLocation));
        Assert.Equal<TypeGenDiagnostic>(diagnostics, TypeGenerationModelValidator.Validate(Model() with { ResourceTypes = [first, second] }));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(32)]
    public void UnknownScopes_AreRejectedForEitherScopeField(int value)
    {
        var resource = Resource("Repository", []);
        AssertCode(Model() with { ResourceTypes = [resource with { ReadableScopes = (ResourceScope)value }] }, "TG2009");
        AssertCode(Model() with { ResourceTypes = [resource with { WritableScopes = (ResourceScope)value }] }, "TG2009");
    }

    [Fact]
    public void AllDefinedScopeAndPropertyFlagCombinations_AreAccepted()
    {
        for (var bits = 0; bits <= 31; bits++)
        {
            var body = Object(new PropertyDefinition { Name = "value", Type = new BooleanTypeDefinition(), Flags = (PropertyFlags)bits });
            var resource = Resource("Repository", []) with { Body = body, ReadableScopes = (ResourceScope)bits, WritableScopes = (ResourceScope)bits };
            Assert.Empty(TypeGenerationModelValidator.Validate(Model() with { ResourceTypes = [resource] }));
        }
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(32)]
    public void UnknownPropertyFlags_AreRejected(int value)
    {
        AssertCode(Model(Object(new PropertyDefinition { Name = "value", Type = new BooleanTypeDefinition(), Flags = (PropertyFlags)value })), "TG2008");
    }

    [Fact]
    public void ResourceAndConfigurationRoots_MustBeObjectLike()
    {
        TypeDefinition[] types = [new AnyTypeDefinition(), new StringTypeDefinition(), new ArrayTypeDefinition { ItemType = new BooleanTypeDefinition() }];
        foreach (var type in types)
        {
            AssertCode(Model() with { ResourceTypes = [Resource("Repository", []) with { Body = type }] }, "TG2005");
            AssertCode(Model() with { Configuration = type }, "TG2005");
        }
        AssertCode(Model() with { ResourceTypes = [Resource("Repository", []) with { Body = null! }] }, "TG2005");
    }

    [Theory]
    [InlineData("resources-default")]
    [InlineData("resource-null")]
    [InlineData("properties-default")]
    [InlineData("property-null")]
    [InlineData("property-type-null")]
    [InlineData("literal-null")]
    [InlineData("array-item-null")]
    [InlineData("union-default")]
    [InlineData("union-empty")]
    [InlineData("element-null")]
    [InlineData("element-type-null")]
    [InlineData("property-versioning-null")]
    [InlineData("property-availability-null")]
    [InlineData("union-availability-null")]
    [InlineData("functions-default")]
    [InlineData("function-null")]
    public void MalformedGraphs_ReturnDiagnosticsInsteadOfThrowing(string scenario)
    {
        var property = new PropertyDefinition { Name = "value", Type = new BooleanTypeDefinition() };
        var model = scenario switch
        {
            "resources-default" => Model() with { ResourceTypes = default },
            "resource-null" => Model() with { ResourceTypes = [null!] },
            "properties-default" => Model(Object() with { Properties = default }),
            "property-null" => Model(Object([null!])),
            "property-type-null" => Model(Object(property with { Type = null! })),
            "literal-null" => WithType(new StringLiteralTypeDefinition { Value = null! }),
            "array-item-null" => WithType(new ArrayTypeDefinition { ItemType = null! }),
            "union-default" => WithType(new UnionTypeDefinition { Elements = default }),
            "union-empty" => WithType(new UnionTypeDefinition { Elements = [] }),
            "element-null" => WithType(new UnionTypeDefinition { Elements = [null!] }),
            "element-type-null" => WithType(new UnionTypeDefinition { Elements = [new() { Type = null! }] }),
            "property-versioning-null" => Model(Object(property with { Versioning = null! })),
            "property-availability-null" => Model(Object(property with { Versioning = new() { Availability = null! } })),
            "union-availability-null" => WithType(new UnionTypeDefinition { Elements = [new() { Type = property.Type, Availability = null! }] }),
            "functions-default" => Model() with { ResourceTypes = [Resource("Repository", []) with { ResourceFunctions = default }] },
            _ => Model() with { ResourceTypes = [Resource("Repository", []) with { ResourceFunctions = [null!] }] },
        };
        AssertCode(model, "TG2006");
    }

    [Fact]
    public void InvalidObjectAndPropertyNames_AreRejected()
    {
        AssertCode(Model(Object() with { Name = "" }), "TG2006");
        AssertCode(Model(Object(new PropertyDefinition { Name = " ", Type = new BooleanTypeDefinition() })), "TG2007");
    }

    [Fact]
    public void DuplicateProperties_ReportBothLocationsInStableOrder()
    {
        var first = new PropertyDefinition { Name = "same", Type = new BooleanTypeDefinition(), SourceLocation = Location("a.cs", 1) };
        var second = first with { SourceLocation = Location("b.cs", 2) };
        var diagnostics = TypeGenerationModelValidator.Validate(Model(Object(second, first)));
        Assert.Equal(2, diagnostics.Length);
        Assert.All(diagnostics, diagnostic => Assert.Equal("TG2007", diagnostic.Code));
        Assert.Equal([first.SourceLocation, second.SourceLocation], diagnostics.Select(diagnostic => diagnostic.SourceLocation));
        Assert.Equal<TypeGenDiagnostic>(diagnostics, TypeGenerationModelValidator.Validate(Model(Object(first, second))));
    }

    [Fact]
    public void InvalidBounds_AreRejectedWithoutInspectingRegexDialect()
    {
        TypeDefinition[] invalid =
        [
            new IntegerTypeDefinition { MinValue = 1, MaxValue = 0 },
            new StringTypeDefinition { MinLength = -1 },
            new StringTypeDefinition { MaxLength = -1 },
            new StringTypeDefinition { MinLength = 2, MaxLength = 1 },
            new ArrayTypeDefinition { ItemType = new AnyTypeDefinition(), MinLength = -1 },
            new ArrayTypeDefinition { ItemType = new AnyTypeDefinition(), MaxLength = -1 },
            new ArrayTypeDefinition { ItemType = new AnyTypeDefinition(), MinLength = 2, MaxLength = 1 },
        ];
        foreach (var type in invalid)
        {
            AssertCode(WithType(type), "TG2010");
        }
        Assert.Empty(TypeGenerationModelValidator.Validate(WithType(new StringTypeDefinition { Pattern = "[" })));
    }

    [Theory]
    [InlineData("added")]
    [InlineData("removed")]
    [InlineData("name")]
    [InlineData("type")]
    [InlineData("required")]
    [InlineData("empty-boundary")]
    public void PropertyLifecycle_IsRejectedWithoutStrippingIt(string scenario)
    {
        var versioning = scenario switch
        {
            "added" => new PropertyVersioning { Availability = new() { AddedAt = "v1" } },
            "removed" => new PropertyVersioning { Availability = new() { RemovedAt = "v2" } },
            "name" => new PropertyVersioning { NameChangedFrom = new() { AtVersion = "v2", PreviousValue = "old" } },
            "type" => new PropertyVersioning { TypeChangedFrom = new() { AtVersion = "v2", PreviousValue = new StringTypeDefinition() } },
            "required" => new PropertyVersioning { RequiredChangedFrom = new() { AtVersion = "v2", PreviousValue = false } },
            _ => new PropertyVersioning { Availability = new() { AddedAt = "" } },
        };
        var property = new PropertyDefinition { Name = "value", Type = new BooleanTypeDefinition(), Versioning = versioning };
        AssertCode(Model(Object(property)), "TG2011");
        Assert.Same(versioning, property.Versioning);
    }

    [Fact]
    public void UnionAvailability_IsRejectedAtTheElementLocation()
    {
        var location = Location("enum.cs", 9);
        var union = new UnionTypeDefinition
        {
            Elements = [new() { Type = new StringLiteralTypeDefinition { Value = "A" }, Availability = new() { RemovedAt = "v2" }, SourceLocation = location }],
        };
        Assert.Equal(location, Assert.Single(TypeGenerationModelValidator.Validate(WithType(union))).SourceLocation);
        AssertCode(WithType(union), "TG2011");
    }

    [Theory]
    [InlineData("preview")]
    [InlineData("deprecated")]
    [InlineData("fallback")]
    [InlineData("function")]
    public void UnsupportedExtensionSemantics_AreExplicitErrors(string scenario)
    {
        var resource = Resource("Repository", ["v1"]);
        var model = scenario switch
        {
            "preview" => Model() with { IsPreview = true },
            "deprecated" => Model() with { IsDeprecated = true },
            "fallback" => Model() with { FallbackResourceType = resource },
            _ => Model() with
            {
                ResourceTypes = [resource with { ResourceFunctions = [new() { Name = "run", OutputType = new BooleanTypeDefinition() }] }],
            },
        };
        AssertCode(model, "TG2011");
    }

    [Fact]
    public void UnsupportedTypes_AreFoundAtRootsAndThroughEverySupportedEdge()
    {
        var unsupported = new DiscriminatedObjectTypeDefinition
        {
            Name = "Shape", Discriminator = "kind", BaseProperties = [], Cases = ImmutableDictionary<string, ObjectTypeDefinition>.Empty,
        };
        TypeDefinition[] nested =
        [
            unsupported,
            new ArrayTypeDefinition { ItemType = unsupported },
            Object() with { AdditionalProperties = unsupported },
            new UnionTypeDefinition { Elements = [new() { Type = unsupported }] },
        ];
        foreach (var type in nested)
        {
            AssertCode(WithType(type), "TG2012");
        }
        AssertCode(Model() with { Configuration = unsupported }, "TG2012");
        AssertCode(Model() with { ResourceTypes = [Resource("Repository", []) with { Body = unsupported }] }, "TG2012");
        AssertCode(WithType(new PoisonType()), "TG2012");
    }

    [Fact]
    public void GraphTraversal_HandlesCyclesSharingAndErrorsAfterBackEdges()
    {
        var node = new ObjectTypeDefinition("Node", self =>
        {
            var peer = new ObjectTypeDefinition("Peer", _ => ([new() { Name = "owner", Type = self }], null));
            return ([
                new() { Name = "aSelf", Type = self },
                new() { Name = "array", Type = new ArrayTypeDefinition(array => array) },
                new() { Name = "peer", Type = peer },
                new() { Name = "union", Type = new UnionTypeDefinition(union => [new() { Type = union }, new() { Type = new NullTypeDefinition() }]) },
                new() { Name = "zError", Type = new IntegerTypeDefinition { MinValue = 5, MaxValue = 2 } },
            ], self);
        });
        var model = Model(node) with { Configuration = node };
        AssertCode(model, "TG2010");
        Assert.Same(node, node.AdditionalProperties);
        Assert.Same(node, node.Properties[0].Type);
    }

    [Fact]
    public void DeepGraphs_DoNotUseRecursiveTraversalOrAFrontendDepthLimit()
    {
        TypeDefinition type = new BooleanTypeDefinition();
        for (var index = 0; index < 4096; index++)
        {
            type = new ArrayTypeDefinition { ItemType = type };
        }
        Assert.Empty(TypeGenerationModelValidator.Validate(WithType(type)));
    }

    [Fact]
    public void SourceLocations_PreferSpecificNodesAndFallbackToTheirOwner()
    {
        var modelLocation = Location("model.cs", 1);
        var resourceLocation = Location("resource.cs", 2);
        var objectLocation = Location("object.cs", 3);
        var propertyLocation = Location("property.cs", 4);
        var typeLocation = Location("type.cs", 5);
        var invalid = new IntegerTypeDefinition { MinValue = 2, MaxValue = 1 };
        ExtensionTypeSemanticModel Create(TypeDefinition value, SourceLocation? propertySource, SourceLocation? objectSource, SourceLocation? resourceSource) =>
            Model() with
            {
                SourceLocation = modelLocation,
                ResourceTypes = [Resource("Repository", []) with
                {
                    SourceLocation = resourceSource,
                    Body = Object(new PropertyDefinition { Name = "value", Type = value, SourceLocation = propertySource }) with { SourceLocation = objectSource },
                }],
            };
        Assert.Equal(typeLocation, Assert.Single(TypeGenerationModelValidator.Validate(
            Create(invalid with { SourceLocation = typeLocation }, propertyLocation, objectLocation, resourceLocation))).SourceLocation);
        Assert.Equal(propertyLocation, Assert.Single(TypeGenerationModelValidator.Validate(
            Create(invalid, propertyLocation, objectLocation, resourceLocation))).SourceLocation);
        Assert.Equal(objectLocation, Assert.Single(TypeGenerationModelValidator.Validate(
            Create(invalid, null, objectLocation, resourceLocation))).SourceLocation);
        Assert.Equal(resourceLocation, Assert.Single(TypeGenerationModelValidator.Validate(
            Create(invalid, null, null, resourceLocation))).SourceLocation);
        Assert.Equal(modelLocation, Assert.Single(TypeGenerationModelValidator.Validate(Create(invalid, null, null, null))).SourceLocation);
    }

    [Fact]
    public void IndependentErrors_AreAggregatedAndSortedWithSourceContext()
    {
        var a = new PropertyDefinition { Name = "a", Type = new IntegerTypeDefinition { MinValue = 2, MaxValue = 1 }, SourceLocation = Location("b.cs", 20) };
        var b = new PropertyDefinition { Name = "b", Type = new BooleanTypeDefinition(), Flags = (PropertyFlags)32, SourceLocation = Location("a.cs", 10) };
        var model = Model(Object(b, a)) with { Name = "" };
        var diagnostics = TypeGenerationModelValidator.Validate(model);
        Assert.Equal(["TG2001", "TG2008", "TG2010"], diagnostics.Select(diagnostic => diagnostic.Code));
        Assert.All(diagnostics, diagnostic =>
        {
            Assert.Equal(TypeGenDiagnosticSeverity.Error, diagnostic.Severity);
            Assert.False(string.IsNullOrWhiteSpace(diagnostic.Message));
        });
        Assert.Equal<TypeGenDiagnostic>(diagnostics, TypeGenerationModelValidator.Validate(Model(Object(a, b)) with { Name = "" }));
    }

    [Fact]
    public async Task RepeatedAndConcurrentCalls_DoNotShareState()
    {
        var invalid = WithType(new IntegerTypeDefinition { MinValue = 2, MaxValue = 1 });
        var expected = TypeGenerationModelValidator.Validate(invalid);
        Assert.Empty(TypeGenerationModelValidator.Validate(Model()));
        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() => TypeGenerationModelValidator.Validate(invalid))));
        Assert.All(results, result => Assert.Equal<TypeGenDiagnostic>(expected, result));
        Assert.Empty(TypeGenerationModelValidator.Validate(Model()));
    }

    [Fact]
    public void NullArgumentsAndCancellation_AreNotConvertedIntoDiagnostics()
    {
        Assert.Equal("model", Assert.Throws<ArgumentNullException>(() => TypeGenerationModelValidator.Validate(null!)).ParamName);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var error = Assert.Throws<OperationCanceledException>(() => TypeGenerationModelValidator.Validate(Model(), cancellation.Token));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Empty(TypeGenerationModelValidator.Validate(Model()));
    }

    private static void AssertCode(ExtensionTypeSemanticModel model, string code)
    {
        var diagnostic = Assert.Single(TypeGenerationModelValidator.Validate(model));
        Assert.Equal(code, diagnostic.Code);
        Assert.Equal(TypeGenDiagnosticSeverity.Error, diagnostic.Severity);
    }

    private static ExtensionTypeSemanticModel WithType(TypeDefinition type) =>
        Model(Object(new PropertyDefinition { Name = "value", Type = type }));

    private static ExtensionTypeSemanticModel Model(ObjectTypeDefinition? body = null) =>
        new() { Name = "Contoso", Version = "1.2", ResourceTypes = [Resource("Repository", []) with { Body = body ?? Object() }] };

    private static ResourceTypeDefinition Resource(string name, ImmutableArray<string> versions) =>
        new() { Name = name, ApiVersions = versions, Body = Object(), ResourceFunctions = [], ReadableScopes = ResourceScope.All, WritableScopes = ResourceScope.All };

    private static ObjectTypeDefinition Object(params PropertyDefinition[] properties) =>
        new() { Name = "Body", Properties = [.. properties] };

    private static SourceLocation Location(string path, int start) => new() { FilePath = path, Start = start, Length = 1 };

    private sealed record PoisonType : TypeDefinition
    {
        public override int GetHashCode() => throw new InvalidOperationException("Structural hashing must not be used.");

        public override string ToString() => throw new InvalidOperationException("Structural display must not be used.");
    }
}
