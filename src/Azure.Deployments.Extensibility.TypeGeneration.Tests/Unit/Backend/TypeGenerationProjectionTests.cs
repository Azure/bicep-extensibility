// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Reflection;
using Azure.Deployments.Extensibility.TypeGeneration.Backend.Projection;
using Azure.Deployments.Extensibility.TypeGeneration.Backend.Validation;
using Azure.Deployments.Extensibility.TypeGeneration.Diagnostics;
using Azure.Deployments.Extensibility.TypeGeneration.Roslyn;
using Azure.Deployments.Extensibility.TypeGeneration.Semantics;
using Azure.Deployments.Extensibility.TypeGeneration.Tests.Support;
using Xunit;

namespace Azure.Deployments.Extensibility.TypeGeneration.Tests.Unit.Backend;

[Trait("Category", "Unit")]
public class TypeGenerationProjectionTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void ZeroOneOrManyVersions_ProduceExactlyTheDeclaredConcreteResources(int count)
    {
        ImmutableArray<string> versions = count switch
        {
            0 => [],
            1 => ["v1"],
            _ => ["v10", "v2", "v1"],
        };
        var source = Resource("Repository", versions);
        var projection = Project(Model(source) with { Version = "not-an-api-version" });
        string?[] expectedVersions = count == 0 ? [null] : [.. versions];
        Assert.Equal(Math.Max(1, count), projection.ResourceTypes.Length);
        Assert.Equal<string?>(expectedVersions, projection.ResourceTypes.Select(resource => resource.ApiVersion));
        foreach (var resource in projection.ResourceTypes)
        {
            Assert.Equal("Repository", resource.Name);
            Assert.Same(source.Body, resource.Body);
            Assert.False(resource.ResourceFunctions.IsDefault);
            Assert.Empty(resource.ResourceFunctions);
        }
        Assert.Equal<string>(versions, source.ApiVersions);
    }

    [Fact]
    public void DefinitionsAndLabels_RetainTheirOriginalOrderWithoutEncodingPublicKeys()
    {
        var first = Resource("Z@opaque", ["v2", "V1", "v1", "  preview@label  "]);
        var second = Resource("A", []);
        var source = Model(first, second);
        var projection = Project(source);
        Assert.Equal<string>(["Z@opaque", "Z@opaque", "Z@opaque", "Z@opaque", "A"],
            projection.ResourceTypes.Select(resource => resource.Name));
        Assert.Equal<string?>(["v2", "V1", "v1", "  preview@label  ", null],
            projection.ResourceTypes.Select(resource => resource.ApiVersion));
        Assert.Same(first, source.ResourceTypes[0]);
        Assert.Same(second, source.ResourceTypes[1]);
        Assert.NotSame(projection.ResourceTypes[0], projection.ResourceTypes[1]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EmptyExtension_IsSuccessfulWithOptionalConfiguration(bool hasConfiguration)
    {
        var configuration = hasConfiguration ? Body("Configuration") : null;
        var source = Model() with { Configuration = configuration, Name = " Contoso ", Version = "not-semver", IsSingleton = true };
        var projection = Project(source);
        Assert.False(projection.ResourceTypes.IsDefault);
        Assert.Empty(projection.ResourceTypes);
        Assert.Equal(source.Name, projection.Name);
        Assert.Equal(source.Version, projection.Version);
        Assert.True(projection.IsSingleton);
        Assert.Same(configuration, projection.Configuration);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(31, 31)]
    [InlineData(15, 0)]
    [InlineData(4, 16)]
    public void ScopesAndSourceLocations_AreCopiedWithoutNormalization(int readable, int writable)
    {
        var sourceLocation = Location("resource.cs", 5);
        var modelLocation = Location("extension.cs", 2);
        var resource = Resource("Repository", ["v1"]) with
        {
            ReadableScopes = (ResourceScope)readable,
            WritableScopes = (ResourceScope)writable,
            SourceLocation = sourceLocation,
        };
        var source = Model(resource) with { SourceLocation = modelLocation };
        var projection = Project(source);
        var projected = Assert.Single(projection.ResourceTypes);
        Assert.Equal((ResourceScope)readable, projected.ReadableScopes);
        Assert.Equal((ResourceScope)writable, projected.WritableScopes);
        Assert.Same(sourceLocation, projected.SourceLocation);
        Assert.Same(modelLocation, projection.SourceLocation);
        Assert.Null(Assert.Single(Project(Model(resource with { SourceLocation = null }) with
        {
            SourceLocation = modelLocation,
        }).ResourceTypes).SourceLocation);
    }

    [Fact]
    public void DisjointDefinitions_PreserveDifferentBodiesAndHaveUniqueTupleIdentities()
    {
        var firstBody = Body("First");
        var secondBody = Body("Second");
        var source = Model(
            Resource("Repository", ["v3", "v1"], firstBody),
            Resource("Repository", ["v2", "V1"], secondBody),
            Resource("repository", ["v1"], secondBody));
        var projection = Project(source);
        Assert.Equal<string?>(["v3", "v1", "v2", "V1", "v1"], projection.ResourceTypes.Select(resource => resource.ApiVersion));
        Assert.Equal(5, projection.ResourceTypes.Select(resource => (resource.Name, resource.ApiVersion)).Distinct().Count());
        Assert.All(projection.ResourceTypes.Take(2), resource => Assert.Same(firstBody, resource.Body));
        Assert.All(projection.ResourceTypes.Skip(2), resource => Assert.Same(secondBody, resource.Body));
    }

    [Fact]
    public void ActualFrontendOutput_PreservesInheritedPropertiesNullableFlagsAndConfiguration()
    {
        var frontend = new TypeGenerationFrontend().Build(
            RoslynCompilationFactory.Create(ContosoAuthoringFixture.Sources.ToArray()), new("Contoso", "1.2"));
        Assert.True(frontend.Succeeded);
        var source = frontend.Model!;
        var input = Assert.Single(source.ResourceTypes);
        var projection = Project(source);
        Assert.Equal(source.Name, projection.Name);
        Assert.Equal(source.Version, projection.Version);
        Assert.Equal(source.IsSingleton, projection.IsSingleton);
        Assert.Same(source.SourceLocation, projection.SourceLocation);
        Assert.Equal(2, projection.ResourceTypes.Length);
        Assert.Same(source.Configuration, projection.Configuration);
        foreach (var resource in projection.ResourceTypes)
        {
            Assert.Same(input.Body, resource.Body);
            Assert.Same(input.SourceLocation, resource.SourceLocation);
            var body = Assert.IsType<ObjectTypeDefinition>(resource.Body);
            var name = Assert.Single(body.Properties, property => property.Name == "name");
            Assert.Equal(PropertyFlags.Required | PropertyFlags.Identifier, name.Flags);
            var description = Assert.Single(body.Properties, property => property.Name == "description");
            var nullable = Assert.IsType<UnionTypeDefinition>(description.Type);
            Assert.Contains(nullable.Elements, element => element.Type is NullTypeDefinition);
            Assert.Equal(PropertyFlags.ReadOnly, Assert.Single(body.Properties, property => property.Name == "url").Flags);
            Assert.All(body.Properties, property =>
            {
                Assert.Null(property.Versioning.Availability.AddedAt);
                Assert.Null(property.Versioning.Availability.RemovedAt);
                Assert.Null(property.Versioning.NameChangedFrom);
                Assert.Null(property.Versioning.TypeChangedFrom);
                Assert.Null(property.Versioning.RequiredChangedFrom);
            });
        }
    }

    [Fact]
    public void ContosoRecursiveSample_RetainsTheCompleteGraphAndDoesNotMutateInput()
    {
        var source = ContosoSampleFixture.CreateExpected();
        var projection = Project(source);
        Assert.Equal(2, projection.ResourceTypes.Length);
        var inputBody = Assert.IsType<ObjectTypeDefinition>(source.ResourceTypes[0].Body);
        Assert.All(projection.ResourceTypes, resource => Assert.Same(inputBody, resource.Body));
        var settings = Assert.IsType<UnionTypeDefinition>(Assert.Single(inputBody.Properties, property => property.Name == "settings").Type);
        var settingsObject = Assert.IsType<ObjectTypeDefinition>(settings.Elements[0].Type);
        Assert.Same(settings, Assert.Single(settingsObject.Properties, property => property.Name == "parent").Type);
        Assert.True(SemanticModelComparer.AreEqual(ContosoSampleFixture.CreateExpected(), source));
    }

    [Fact]
    public void SharedCyclicGraph_AcrossResourcesVersionsAndConfiguration_IsNotCloned()
    {
        var body = new ObjectTypeDefinition("Node", self =>
        {
            var peer = new ObjectTypeDefinition("Peer", _ => ([new() { Name = "owner", Type = self }], null));
            return ([new() { Name = "peer", Type = peer }, new() { Name = "self", Type = self }], self);
        });
        var projection = Project(Model(Resource("First", ["b", "a"], body), Resource("Second", [], body)) with { Configuration = body });
        Assert.Same(body, projection.Configuration);
        Assert.Equal(3, projection.ResourceTypes.Length);
        Assert.All(projection.ResourceTypes, resource => Assert.Same(body, resource.Body));
        Assert.Same(body, body.AdditionalProperties);
        Assert.Same(body, ((ObjectTypeDefinition)body.Properties[0].Type).Properties[0].Type);
    }

    [Fact]
    public void DeepBody_IsReusedForEveryVersion()
    {
        TypeDefinition type = new BooleanTypeDefinition();
        for (var index = 0; index < 4096; index++)
        {
            type = new ArrayTypeDefinition { ItemType = type };
        }
        var body = Body("Deep") with { Properties = [new() { Name = "value", Type = type }] };
        var projection = Project(Model(Resource("Repository", ["v1", "v2"], body)));
        Assert.All(projection.ResourceTypes, resource =>
        {
            Assert.Same(body, resource.Body);
            Assert.Same(type, Assert.Single(((ObjectTypeDefinition)resource.Body).Properties).Type);
        });
    }

    [Theory]
    [InlineData("default")]
    [InlineData("null")]
    [InlineData("blank")]
    [InlineData("repeated")]
    public void InvalidVersions_FailTheEntireProjection(string scenario)
    {
        ImmutableArray<string> versions = scenario switch
        {
            "default" => default,
            "null" => [null!],
            "blank" => [" "],
            _ => ["v1", "v1"],
        };
        var invalid = Resource("Invalid", versions) with { SourceLocation = Location("invalid.cs", 10) };
        var result = AssertFailure(Model(Resource("Valid", ["v0"]), invalid), "TG2003");
        Assert.Equal(invalid.SourceLocation, Assert.Single(result.Diagnostics).SourceLocation);
    }

    [Theory]
    [InlineData("overlap")]
    [InlineData("unversioned")]
    [InlineData("mixed")]
    public void RegistrationConflicts_KeepBothLocationsAndExposeNoPartialResources(string scenario)
    {
        var first = Resource("Repository", scenario == "unversioned" ? [] : ["v1"]) with { SourceLocation = Location("a.cs", 1) };
        var second = Resource("Repository", scenario == "overlap" ? ["v1"] : []) with { SourceLocation = Location("b.cs", 2) };
        var result = AssertFailure(Model(Resource("Valid", ["v0"]), first, second), "TG2004");
        Assert.Equal<SourceLocation?>([first.SourceLocation, second.SourceLocation], result.Diagnostics.Select(diagnostic => diagnostic.SourceLocation));
    }

    [Theory]
    [InlineData("resource")]
    [InlineData("configuration")]
    [InlineData("metadata")]
    [InlineData("constraint")]
    public void ValidationCoversTheWholeExtensionBeforeAnyProjectionIsReturned(string scenario)
    {
        var model = Model(Resource("Repository", ["v1"]));
        var invalid = scenario switch
        {
            "resource" => model with { ResourceTypes = [model.ResourceTypes[0] with { Body = new StringTypeDefinition() }] },
            "configuration" => model with { Configuration = new StringTypeDefinition() },
            "metadata" => model with { Name = "" },
            _ => model with
            {
                Configuration = Body("Config") with
                {
                    Properties = [new() { Name = "count", Type = new IntegerTypeDefinition { MinValue = 2, MaxValue = 1 } }],
                },
            },
        };
        AssertFailure(invalid, scenario is "resource" or "configuration" ? "TG2005" : scenario == "metadata" ? "TG2001" : "TG2010");
    }

    [Theory]
    [InlineData("added")]
    [InlineData("removed")]
    [InlineData("rename")]
    [InlineData("type")]
    [InlineData("required")]
    [InlineData("union")]
    public void Lifecycle_IsRejectedInsteadOfPublishingTheLatestSchemaToAllVersions(string scenario)
    {
        var versioning = scenario switch
        {
            "added" => new PropertyVersioning { Availability = new() { AddedAt = "v2" } },
            "removed" => new PropertyVersioning { Availability = new() { RemovedAt = "v2" } },
            "rename" => new PropertyVersioning { NameChangedFrom = new() { AtVersion = "v2", PreviousValue = "previous" } },
            "type" => new PropertyVersioning { TypeChangedFrom = new() { AtVersion = "v2", PreviousValue = new IntegerTypeDefinition() } },
            "required" => new PropertyVersioning { RequiredChangedFrom = new() { AtVersion = "v2", PreviousValue = false } },
            _ => new PropertyVersioning(),
        };
        TypeDefinition type = scenario == "union"
            ? new UnionTypeDefinition
            {
                Elements = [new() { Type = new StringLiteralTypeDefinition { Value = "Late" }, Availability = new() { AddedAt = "v2" } }],
            }
            : new StringTypeDefinition();
        var property = new PropertyDefinition { Name = "current", Type = type, Versioning = versioning, SourceLocation = Location("model.cs", 7) };
        var body = Body("Repository") with { Properties = [property] };
        var source = Model(Resource("Repository", ["v1", "v2"], body));
        var result = AssertFailure(source, "TG2011");
        Assert.Equal(property.SourceLocation, Assert.Single(result.Diagnostics).SourceLocation);
        Assert.Same(versioning, property.Versioning);
        Assert.Same(type, property.Type);
        Assert.Same(body, source.ResourceTypes[0].Body);
    }

    [Theory]
    [InlineData("function")]
    [InlineData("fallback")]
    [InlineData("preview")]
    [InlineData("deprecated")]
    [InlineData("discriminated")]
    public void UnsupportedBranches_AreNotSilentlyDropped(string scenario)
    {
        var resource = Resource("Repository", ["v1"]);
        var source = scenario switch
        {
            "function" => Model(resource with { ResourceFunctions = [new() { Name = "run", OutputType = new BooleanTypeDefinition() }] }),
            "fallback" => Model(resource) with { FallbackResourceType = resource },
            "preview" => Model(resource) with { IsPreview = true },
            "deprecated" => Model(resource) with { IsDeprecated = true },
            _ => Model(resource with
            {
                Body = new DiscriminatedObjectTypeDefinition
                {
                    Name = "Shape", Discriminator = "kind", BaseProperties = [], Cases = ImmutableDictionary<string, ObjectTypeDefinition>.Empty,
                },
            }),
        };
        AssertFailure(source, scenario == "discriminated" ? "TG2012" : "TG2011");
    }

    [Fact]
    public async Task RepeatedAndConcurrentProjections_ShareOnlyTheImmutableInputGraph()
    {
        var source = ContosoSampleFixture.CreateExpected();
        var first = Project(source);
        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() => Project(source))));
        Assert.All(results, result =>
        {
            Assert.NotSame(first, result);
            Assert.Equal<string?>(first.ResourceTypes.Select(resource => resource.ApiVersion), result.ResourceTypes.Select(resource => resource.ApiVersion));
            Assert.Same(source.ResourceTypes[0].Body, result.ResourceTypes[0].Body);
            Assert.NotSame(first.ResourceTypes[0], result.ResourceTypes[0]);
        });
        AssertFailure(source with { Name = "" }, "TG2001");
        Assert.Equal(2, Project(source).ResourceTypes.Length);
    }

    [Fact]
    public void NullArgumentsAndCancellation_DoNotReturnProjectionResults()
    {
        Assert.Equal("model", Assert.Throws<ArgumentNullException>(() => TypeGenerationProjectionService.Project(null!)).ParamName);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        foreach (var source in new[] { Model(), Model(Resource("Repository", ["v1"])), Model() with { Name = "" } })
        {
            var error = Assert.Throws<OperationCanceledException>(() => TypeGenerationProjectionService.Project(source, cancellation.Token));
            Assert.Equal(cancellation.Token, error.CancellationToken);
        }
        Assert.Empty(Project(Model()).ResourceTypes);
    }

    [Fact]
    public void ProjectionContracts_AreInternalAndUseOnlyTheExistingSemanticTypes()
    {
        Assert.False(typeof(TypeGenerationProjectionService).IsPublic);
        Assert.False(typeof(TypeGenerationProjectionResult).IsPublic);
        Assert.False(typeof(TypeGenerationProjectedExtension).IsPublic);
        var method = Assert.Single(typeof(TypeGenerationProjectionService).GetMethods(BindingFlags.Public | BindingFlags.Static));
        Assert.Equal("Project", method.Name);
        Assert.Equal(typeof(TypeGenerationProjectionResult), method.ReturnType);
        Assert.Equal<Type>([typeof(ExtensionTypeSemanticModel), typeof(CancellationToken)], method.GetParameters().Select(parameter => parameter.ParameterType));
        Assert.True(method.GetParameters()[1].IsOptional);
        Assert.Equal(typeof(ImmutableArray<ProjectedResourceTypeDefinition>),
            typeof(TypeGenerationProjectedExtension).GetProperty(nameof(TypeGenerationProjectedExtension.ResourceTypes))!.PropertyType);
        Assert.All(typeof(TypeGenerationProjectedExtension).GetProperties(), property => Assert.Null(property.SetMethod));
    }

    [Fact]
    public void ProjectionResult_RejectsMalformedDiagnosticsAndPartialSuccess()
    {
        var projection = Project(Model());
        Assert.Throws<ArgumentException>(() => new TypeGenerationProjectionResult(null, default));
        Assert.Throws<ArgumentException>(() => new TypeGenerationProjectionResult(null, [null!]));
        Assert.Throws<ArgumentException>(() => new TypeGenerationProjectionResult(projection,
            [new("TG2003", TypeGenDiagnosticSeverity.Error, "Invalid versions.", null)]));
        Assert.False(new TypeGenerationProjectionResult(null, []).Succeeded);
    }

    [Theory]
    [InlineData(TypeGenDiagnosticSeverity.Info)]
    [InlineData(TypeGenDiagnosticSeverity.Warning)]
    public void CompleteProjection_PreservesNonErrorDiagnostics(TypeGenDiagnosticSeverity severity)
    {
        var projection = Project(Model());
        var diagnostic = new TypeGenDiagnostic("TG2003", severity, "Example.", null);
        var result = new TypeGenerationProjectionResult(projection, [diagnostic]);
        Assert.True(result.Succeeded);
        Assert.Same(projection, result.Model);
        Assert.Same(diagnostic, Assert.Single(result.Diagnostics));
    }

    [Fact]
    public void ProjectedExtension_RejectsMissingSourceOrUninitializedResources()
    {
        Assert.Throws<ArgumentNullException>(() => new TypeGenerationProjectedExtension(null!, []));
        Assert.Throws<ArgumentException>(() => new TypeGenerationProjectedExtension(Model(), default));
        Assert.Throws<ArgumentException>(() => new TypeGenerationProjectedExtension(Model(), [null!]));
    }

    private static TypeGenerationProjectedExtension Project(ExtensionTypeSemanticModel source)
    {
        var result = TypeGenerationProjectionService.Project(source);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        return Assert.IsType<TypeGenerationProjectedExtension>(result.Model);
    }

    private static TypeGenerationProjectionResult AssertFailure(ExtensionTypeSemanticModel source, string code)
    {
        var result = TypeGenerationProjectionService.Project(source);
        Assert.False(result.Succeeded);
        Assert.Null(result.Model);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == code && diagnostic.Severity == TypeGenDiagnosticSeverity.Error);
        Assert.Equal<TypeGenDiagnostic>(TypeGenerationModelValidator.Validate(source), result.Diagnostics);
        return result;
    }

    private static ExtensionTypeSemanticModel Model(params ResourceTypeDefinition[] resources) =>
        new() { Name = "Contoso", Version = "1.2", ResourceTypes = [.. resources] };

    private static ResourceTypeDefinition Resource(string name, ImmutableArray<string> versions, ObjectTypeDefinition? body = null) =>
        new()
        {
            Name = name,
            ApiVersions = versions,
            ReadableScopes = ResourceScope.All,
            WritableScopes = ResourceScope.All,
            Body = body ?? Body("Body"),
            ResourceFunctions = [],
        };

    private static ObjectTypeDefinition Body(string name) => new() { Name = name, Properties = [] };

    private static SourceLocation Location(string path, int start) => new() { FilePath = path, Start = start, Length = 1 };
}
