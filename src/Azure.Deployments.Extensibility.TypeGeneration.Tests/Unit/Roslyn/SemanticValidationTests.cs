// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.TypeGeneration.Diagnostics;
using Azure.Deployments.Extensibility.TypeGeneration.Roslyn.Validation;
using Azure.Deployments.Extensibility.TypeGeneration.Semantics;
using Azure.Deployments.Extensibility.TypeGeneration.Tests.Support;
using Xunit;

namespace Azure.Deployments.Extensibility.TypeGeneration.Tests.Unit.Roslyn;

[Trait("Category", "Unit")]
public class SemanticValidationTests
{
    private static readonly SourceLocation Location = new() { FilePath = "Fixture.cs", Start = 10, Length = 5 };

    [Fact]
    public void GoldenAndEmptyExtension_AreValidPhaseOneModels()
    {
        Assert.Empty(PhaseOneModelValidator.Validate(ContosoAuthoringFixture.CreateExpected()));
        Assert.Empty(PhaseOneModelValidator.Validate(new() { Name = "E", Version = "1", ResourceTypes = [] }));
    }

    [Theory]
    [InlineData("extension-name")]
    [InlineData("extension-version")]
    [InlineData("preview")]
    [InlineData("deprecated")]
    [InlineData("fallback")]
    [InlineData("resource-array")]
    [InlineData("null-resource")]
    [InlineData("resource-name")]
    [InlineData("duplicate-resource")]
    [InlineData("version-array")]
    [InlineData("duplicate-version")]
    [InlineData("blank-version")]
    [InlineData("read-scopes")]
    [InlineData("write-scopes")]
    [InlineData("function-array")]
    [InlineData("functions")]
    [InlineData("body-null")]
    [InlineData("body-scalar")]
    [InlineData("body-map")]
    [InlineData("configuration-scalar")]
    public void InvalidRootsAndRegistrations_AreExplicitErrors(string scenario)
    {
        var model = CreateModel(new ObjectTypeDefinition { Name = "Body", Properties = [] });
        var resource = model.ResourceTypes[0];
        model = scenario switch
        {
            "extension-name" => model with { Name = "" },
            "extension-version" => model with { Version = " " },
            "preview" => model with { IsPreview = true },
            "deprecated" => model with { IsDeprecated = true },
            "fallback" => model with { FallbackResourceType = resource },
            "resource-array" => model with { ResourceTypes = default },
            "null-resource" => model with { ResourceTypes = [null!] },
            "resource-name" => model with { ResourceTypes = [resource with { Name = "" }] },
            "duplicate-resource" => model with { ResourceTypes = [resource, resource] },
            "version-array" => model with { ResourceTypes = [resource with { ApiVersions = default }] },
            "duplicate-version" => model with { ResourceTypes = [resource with { ApiVersions = ["v1", "v1"] }] },
            "blank-version" => model with { ResourceTypes = [resource with { ApiVersions = [" "] }] },
            "read-scopes" => model with { ResourceTypes = [resource with { ReadableScopes = ResourceScope.None }] },
            "write-scopes" => model with { ResourceTypes = [resource with { WritableScopes = ResourceScope.ResourceGroup }] },
            "function-array" => model with { ResourceTypes = [resource with { ResourceFunctions = default }] },
            "functions" => model with { ResourceTypes = [resource with { ResourceFunctions = [null!] }] },
            "body-null" => model with { ResourceTypes = [resource with { Body = null! }] },
            "body-scalar" => model with { ResourceTypes = [resource with { Body = new StringTypeDefinition() }] },
            "body-map" => model with { ResourceTypes = [resource with
            {
                Body = new ObjectTypeDefinition { Name = "Map", Properties = [], AdditionalProperties = new StringTypeDefinition() },
            }] },
            "configuration-scalar" => model with { Configuration = new StringTypeDefinition() },
            _ => throw new InvalidOperationException($"Unknown scenario {scenario}."),
        };
        AssertErrors(model);
    }

    [Theory]
    [InlineData("object-name")]
    [InlineData("object-sensitive")]
    [InlineData("properties-default")]
    [InlineData("null-property")]
    [InlineData("property-name")]
    [InlineData("duplicate-property")]
    [InlineData("property-flags")]
    [InlineData("property-type")]
    [InlineData("property-versioning")]
    [InlineData("property-availability")]
    [InlineData("property-rename")]
    [InlineData("property-type-change")]
    [InlineData("property-required-change")]
    [InlineData("map-and-fixed")]
    public void InvalidObjectAndPropertyMetadata_AreNotSilentlyAccepted(string scenario)
    {
        var property = new PropertyDefinition { Name = "value", Type = new StringTypeDefinition() };
        var body = new ObjectTypeDefinition { Name = "Body", Properties = [property] };
        body = scenario switch
        {
            "object-name" => body with { Name = "" },
            "object-sensitive" => body with { Sensitive = true },
            "properties-default" => body with { Properties = default },
            "null-property" => body with { Properties = [null!] },
            "property-name" => body with { Properties = [property with { Name = " " }] },
            "duplicate-property" => body with { Properties = [property, property] },
            "property-flags" => body with { Properties = [property with { Flags = (PropertyFlags)32 }] },
            "property-type" => body with { Properties = [property with { Type = null! }] },
            "property-versioning" => body with { Properties = [property with { Versioning = null! }] },
            "property-availability" => body with { Properties = [property with
            {
                Versioning = new() { Availability = new() { AddedAt = "v1" } },
            }] },
            "property-rename" => body with { Properties = [property with
            {
                Versioning = new() { NameChangedFrom = new() { AtVersion = "v1", PreviousValue = "old" } },
            }] },
            "property-type-change" => body with { Properties = [property with
            {
                Versioning = new() { TypeChangedFrom = new() { AtVersion = "v1", PreviousValue = new BooleanTypeDefinition() } },
            }] },
            "property-required-change" => body with { Properties = [property with
            {
                Versioning = new() { RequiredChangedFrom = new() { AtVersion = "v1", PreviousValue = true } },
            }] },
            "map-and-fixed" => body with { AdditionalProperties = new StringTypeDefinition() },
            _ => throw new InvalidOperationException($"Unknown scenario {scenario}."),
        };
        AssertErrors(CreateModel(body));
    }

    [Theory]
    [InlineData("any")]
    [InlineData("discriminator")]
    [InlineData("integer-missing")]
    [InlineData("integer-reversed")]
    [InlineData("string-constraints")]
    [InlineData("array-constraints")]
    [InlineData("array-null-edge")]
    [InlineData("union-default")]
    [InlineData("union-empty")]
    [InlineData("union-null-element")]
    [InlineData("union-null-edge")]
    [InlineData("heterogeneous-union")]
    [InlineData("duplicate-literal")]
    [InlineData("empty-literal")]
    [InlineData("union-lifecycle")]
    [InlineData("standalone-null")]
    [InlineData("standalone-literal")]
    public void UnsupportedOrIncompleteTypeNodes_AreErrors(string scenario)
    {
        TypeDefinition type = scenario switch
        {
            "any" => new AnyTypeDefinition(),
            "discriminator" => new DiscriminatedObjectTypeDefinition
            {
                Name = "Shape", Discriminator = "kind", BaseProperties = [], Cases = System.Collections.Immutable.ImmutableDictionary<string, ObjectTypeDefinition>.Empty,
            },
            "integer-missing" => new IntegerTypeDefinition(),
            "integer-reversed" => new IntegerTypeDefinition { MinValue = 2, MaxValue = 1 },
            "string-constraints" => new StringTypeDefinition { Pattern = "future" },
            "array-constraints" => new ArrayTypeDefinition { ItemType = new StringTypeDefinition(), MaxLength = 3 },
            "array-null-edge" => new ArrayTypeDefinition { ItemType = null! },
            "union-default" => new UnionTypeDefinition { Elements = default },
            "union-empty" => new UnionTypeDefinition { Elements = [] },
            "union-null-element" => new UnionTypeDefinition { Elements = [null!] },
            "union-null-edge" => new UnionTypeDefinition { Elements = [new() { Type = null! }] },
            "heterogeneous-union" => new UnionTypeDefinition
            {
                Elements = [new() { Type = new StringTypeDefinition() }, new() { Type = new BooleanTypeDefinition() }],
            },
            "duplicate-literal" => new UnionTypeDefinition
            {
                Elements = [new() { Type = new StringLiteralTypeDefinition { Value = "A" } }, new() { Type = new StringLiteralTypeDefinition { Value = "A" } }],
            },
            "empty-literal" => new UnionTypeDefinition { Elements = [new() { Type = new StringLiteralTypeDefinition { Value = "" } }] },
            "union-lifecycle" => new UnionTypeDefinition
            {
                Elements = [new() { Type = new StringLiteralTypeDefinition { Value = "A" }, Availability = new() { RemovedAt = "v1" } }],
            },
            "standalone-null" => new NullTypeDefinition(),
            "standalone-literal" => new StringLiteralTypeDefinition { Value = "A" },
            _ => throw new InvalidOperationException($"Unknown scenario {scenario}."),
        };
        AssertErrors(CreateModel(new ObjectTypeDefinition
        {
            Name = "Body", Properties = [new() { Name = "value", Type = type }],
        }));
    }

    [Fact]
    public void SharedLiteralAndNull_StillRequireTheRightOccurrenceContext()
    {
        var literal = new StringLiteralTypeDefinition { Value = "A" };
        var nullType = new NullTypeDefinition();
        var model = CreateModel(new ObjectTypeDefinition
        {
            Name = "Body",
            Properties =
            [
                new() { Name = "enum", Type = new UnionTypeDefinition { Elements = [new() { Type = literal }] } },
                new() { Name = "nullable", Type = new UnionTypeDefinition
                {
                    Elements = [new() { Type = new StringTypeDefinition() }, new() { Type = nullType }],
                } },
                new() { Name = "badLiteral", Type = literal },
                new() { Name = "badNull", Type = nullType },
            ],
        });
        Assert.Equal(2, PhaseOneModelValidator.Validate(model).Length);
    }

    [Fact]
    public void InvalidCyclicNode_IsReportedOnceWithoutRecursiveRecordOperations()
    {
        var body = new ObjectTypeDefinition("Body", self =>
            ([new PropertyDefinition { Name = "self", Type = self, Flags = (PropertyFlags)32 }], null));
        var diagnostics = PhaseOneModelValidator.Validate(CreateModel(body));
        Assert.Single(diagnostics);
        Assert.Equal(TypeGenDiagnosticCodes.InvalidSemanticModel, diagnostics[0].Code);
        Assert.Equal(Location, diagnostics[0].SourceLocation);
    }

    [Fact]
    public void DeepAlreadyConstructedGraph_IsValidatedIteratively()
    {
        TypeDefinition body = new ObjectTypeDefinition { Name = "End", Properties = [] };
        for (var index = 0; index < 5000; index++)
        {
            body = new ObjectTypeDefinition
            {
                Name = $"Node{index}", Properties = [new() { Name = "next", Type = body }],
            };
        }
        Assert.Empty(PhaseOneModelValidator.Validate(CreateModel(body)));
    }

    [Fact]
    public void Validator_RejectsNullArgumentsAndHonorsCancellation()
    {
        Assert.Throws<ArgumentNullException>(() => PhaseOneModelValidator.Validate(null!));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => PhaseOneModelValidator.Validate(
            ContosoAuthoringFixture.CreateExpected(), cancellation.Token));
    }

    private static ExtensionTypeSemanticModel CreateModel(TypeDefinition body) => new()
    {
        Name = "E",
        Version = "1",
        SourceLocation = Location,
        ResourceTypes =
        [
            new()
            {
                Name = "R", ApiVersions = [], ReadableScopes = ResourceScope.All,
                WritableScopes = ResourceScope.All, Body = body, ResourceFunctions = [],
            },
        ],
    };

    private static void AssertErrors(ExtensionTypeSemanticModel model)
    {
        var diagnostics = PhaseOneModelValidator.Validate(model);
        Assert.NotEmpty(diagnostics);
        Assert.All(diagnostics, diagnostic =>
        {
            Assert.Equal(TypeGenDiagnosticCodes.InvalidSemanticModel, diagnostic.Code);
            Assert.Equal(TypeGenDiagnosticSeverity.Error, diagnostic.Severity);
            Assert.Equal(Location, diagnostic.SourceLocation);
        });
    }
}
