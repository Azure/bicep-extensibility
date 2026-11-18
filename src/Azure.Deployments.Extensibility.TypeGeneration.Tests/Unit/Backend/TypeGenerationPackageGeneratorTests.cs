// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Reflection;
using Azure.Deployments.Extensibility.TypeGeneration.Backend;
using Azure.Deployments.Extensibility.TypeGeneration.Backend.Packaging;
using Azure.Deployments.Extensibility.TypeGeneration.Backend.Validation;
using Azure.Deployments.Extensibility.TypeGeneration.Backend.Serialization;
using Azure.Deployments.Extensibility.TypeGeneration.Roslyn;
using Azure.Deployments.Extensibility.TypeGeneration.Semantics;
using Azure.Deployments.Extensibility.TypeGeneration.Tests.Support;
using Xunit;

namespace Azure.Deployments.Extensibility.TypeGeneration.Tests.Unit.Backend;

[Trait("Category", "Unit")]
public class TypeGenerationPackageGeneratorTests
{
    [Fact]
    public void PublicEntryPoint_IsSynchronousAndRequiresNoProjectHost()
    {
        var method = Assert.Single(typeof(TypeGenerationPackageGenerator).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));
        Assert.Equal("Generate", method.Name);
        Assert.Equal(typeof(TypeGenerationPackageResult), method.ReturnType);
        Assert.Equal<Type>([typeof(ExtensionTypeSemanticModel), typeof(CancellationToken)], method.GetParameters().Select(parameter => parameter.ParameterType));
        Assert.True(method.GetParameters()[1].IsOptional);
    }

    [Fact]
    public void ActualFrontend_GeneratesACompleteVerifiedArchive()
    {
        var frontend = new TypeGenerationFrontend().Build(RoslynCompilationFactory.Create(ContosoSampleFixture.ReadSources()), new("Contoso", "1.2"));
        Assert.True(frontend.Succeeded);
        var result = new TypeGenerationPackageGenerator().Generate(frontend.Model!);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
        var package = Assert.IsType<TypeGenerationPackage>(result.Package);
        var json = TypeGenerationPackageValidator.Validate(new(package.IndexJson, package.TypesJson));
        Assert.True(json.Succeeded);
        Assert.True(TypeGenerationArchiveValidator.Validate(package.ArchiveBytes, json.Model!).Succeeded);
    }

    [Theory]
    [InlineData("metadata", "TG2001")]
    [InlineData("lifecycle", "TG2011")]
    [InlineData("identity", "TG2201")]
    public void FailedEarlierStages_ReturnNoPackage(string scenario, string code)
    {
        var source = SemanticModelFixtures.CreateContoso();
        var resource = source.ResourceTypes[0];
        source = scenario switch
        {
            "metadata" => source with { Name = "" },
            "lifecycle" => source with { IsPreview = true },
            _ => source with { ResourceTypes = [resource with { Name = "R", ApiVersions = ["v1"] }, resource with { Name = "R@v1", ApiVersions = [] }] },
        };
        var result = new TypeGenerationPackageGenerator().Generate(source);
        Assert.False(result.Succeeded);
        Assert.Null(result.Package);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == code);
    }

    [Fact]
    public async Task ConcurrentCallsOnOneGenerator_AreDeterministicAndDoNotMutateSource()
    {
        var source = ContosoSampleFixture.CreateExpected();
        var generator = new TypeGenerationPackageGenerator();
        var expected = generator.Generate(source);
        Assert.True(expected.Succeeded);
        var results = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => Task.Run(() => generator.Generate(source))));
        Assert.All(results, result =>
        {
            Assert.True(result.Succeeded);
            Assert.Equal<byte>(expected.Package!.ArchiveBytes, result.Package!.ArchiveBytes);
        });
        Assert.True(SemanticModelComparer.AreEqual(ContosoSampleFixture.CreateExpected(), source));
    }

    [Fact]
    public void NullAndCancellation_AreExceptions()
    {
        var generator = new TypeGenerationPackageGenerator();
        Assert.Equal("model", Assert.Throws<ArgumentNullException>(() => generator.Generate(null!)).ParamName);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var error = Assert.Throws<OperationCanceledException>(() => generator.Generate(SemanticModelFixtures.CreateContoso(), cancellation.Token));
        Assert.Equal(cancellation.Token, error.CancellationToken);
    }
}
