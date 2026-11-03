// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Text.Json;
using Azure.Deployments.Extensibility.TypeGeneration.Backend;
using Azure.Deployments.Extensibility.TypeGeneration.Backend.Validation;
using Azure.Deployments.Extensibility.TypeGeneration.Roslyn;
using Azure.Deployments.Extensibility.TypeGeneration.Semantics;
using Azure.Deployments.Extensibility.TypeGeneration.Tests.Support;
using Xunit;

namespace Azure.Deployments.Extensibility.TypeGeneration.Tests.Unit.Backend;

[Trait("Category", "Unit")]
public class TypeGenerationJsonGeneratorTests
{
    [Fact]
    public void ActualFrontend_ReachesValidatedJsonWithNoFunctionIndexes()
    {
        var frontend = new TypeGenerationFrontend().Build(
            RoslynCompilationFactory.Create(ContosoSampleFixture.ReadSources()), new("Contoso", "1.2"));
        Assert.True(frontend.Succeeded);
        var result = TypeGenerationJsonGenerator.Generate(frontend.Model!);
        Assert.True(result.Succeeded, string.Join(Environment.NewLine, result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        Assert.Empty(result.Diagnostics);
        var files = result.Model!.Files;
        using var index = JsonDocument.Parse(files.IndexJson.AsMemory());
        using var types = JsonDocument.Parse(files.TypesJson.AsMemory());
        Assert.Equal(2, index.RootElement.GetProperty("resources").EnumerateObject().Count());
        Assert.False(index.RootElement.TryGetProperty("resourceFunctions", out _));
        Assert.False(index.RootElement.TryGetProperty("namespaceFunctions", out _));
        Assert.Equal(JsonValueKind.Array, types.RootElement.ValueKind);
        Assert.True(TypeGenerationPackageValidator.Validate(files).Succeeded);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EmptyAndConfigurationOnlyInputs_GenerateValidatedPackages(bool configuration)
    {
        var source = new ExtensionTypeSemanticModel
        {
            Name = "Empty", Version = "not-semver", ResourceTypes = [],
            Configuration = configuration ? new ObjectTypeDefinition { Name = "Config", Properties = [] } : null,
        };
        var result = TypeGenerationJsonGenerator.Generate(source);
        Assert.True(result.Succeeded);
        using var index = JsonDocument.Parse(result.Model!.Files.IndexJson.AsMemory());
        Assert.Empty(index.RootElement.GetProperty("resources").EnumerateObject());
        Assert.Equal(configuration, index.RootElement.GetProperty("settings").TryGetProperty("configurationType", out _));
    }

    [Fact]
    public void OrdinaryBusinessNames_CanMatchOmittedFormatFields()
    {
        var source = SemanticModelFixtures.CreateContoso();
        var resource = source.ResourceTypes[0];
        var body = (ObjectTypeDefinition)resource.Body;
        source = source with
        {
            ResourceTypes = [resource with
            {
                Body = body with { Properties = [
                    new() { Name = "resourceFunctions", Type = new StringTypeDefinition() },
                    new() { Name = "namespaceFunctions", Type = new BooleanTypeDefinition() },
                ] },
            }],
        };
        Assert.True(TypeGenerationJsonGenerator.Generate(source).Succeeded);
    }

    [Theory]
    [InlineData("metadata", "TG2001")]
    [InlineData("lifecycle", "TG2011")]
    [InlineData("encoded-key", "TG2201")]
    public void FailuresAtEarlierStages_DoNotReturnValidatedFiles(string scenario, string code)
    {
        var model = SemanticModelFixtures.CreateContoso();
        var resource = model.ResourceTypes[0];
        var source = scenario switch
        {
            "metadata" => model with { Name = "" },
            "lifecycle" => model with
            {
                ResourceTypes = [resource with
                {
                    Body = new ObjectTypeDefinition
                    {
                        Name = "Body", Properties = [new()
                        {
                            Name = "later", Type = new BooleanTypeDefinition(),
                            Versioning = new() { Availability = new() { AddedAt = resource.ApiVersions[0] } },
                        }],
                    },
                }],
            },
            _ => model with
            {
                ResourceTypes = [resource with { Name = "R", ApiVersions = ["v1"] }, resource with { Name = "R@v1", ApiVersions = [] }],
            },
        };
        var result = TypeGenerationJsonGenerator.Generate(source);
        Assert.False(result.Succeeded);
        Assert.Null(result.Model);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == code);
    }

    [Fact]
    public void CyclicAndDeepInputs_ValidateWithoutRecursiveGraphExpansion()
    {
        var model = ContosoSampleFixture.CreateExpected();
        Assert.True(TypeGenerationJsonGenerator.Generate(model).Succeeded);
        TypeDefinition type = new BooleanTypeDefinition();
        for (var depth = 0; depth < 4096; depth++) { type = new ArrayTypeDefinition { ItemType = type }; }
        var resource = model.ResourceTypes[0] with
        {
            Body = new ObjectTypeDefinition { Name = "Deep", Properties = [new() { Name = "value", Type = type }] },
        };
        Assert.True(TypeGenerationJsonGenerator.Generate(model with { ResourceTypes = [resource] }).Succeeded);
    }

    [Fact]
    public async Task RepeatedConcurrentGeneration_IsDeterministicAndReadOnly()
    {
        var source = ContosoSampleFixture.CreateExpected();
        var expected = TypeGenerationJsonGenerator.Generate(source);
        Assert.True(expected.Succeeded);
        var results = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => Task.Run(() => TypeGenerationJsonGenerator.Generate(source))));
        Assert.All(results, result =>
        {
            Assert.True(result.Succeeded);
            Assert.Equal<byte>(expected.Model!.Files.IndexJson, result.Model!.Files.IndexJson);
            Assert.Equal<byte>(expected.Model.Files.TypesJson, result.Model.Files.TypesJson);
        });
        Assert.True(SemanticModelComparer.AreEqual(ContosoSampleFixture.CreateExpected(), source));
    }

    [Fact]
    public void NullAndCancellation_AreNotTurnedIntoEmptySuccess()
    {
        Assert.Equal("model", Assert.Throws<ArgumentNullException>(() => TypeGenerationJsonGenerator.Generate(null!)).ParamName);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var error = Assert.Throws<OperationCanceledException>(() => TypeGenerationJsonGenerator.Generate(SemanticModelFixtures.CreateContoso(), cancellation.Token));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.False(typeof(TypeGenerationJsonGenerator).IsPublic);
    }
}
