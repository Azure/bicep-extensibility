// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using System.Xml.Linq;
using Azure.Deployments.Extensibility.TypeGeneration.Roslyn;
using Azure.Deployments.Extensibility.TypeGeneration.Semantics;
using Azure.Deployments.Extensibility.TypeGeneration.Tests.Support;
using Azure.Deployments.Extensibility.TypeGeneration.Tool;
using Xunit;

namespace Azure.Deployments.Extensibility.TypeGeneration.Tests.Unit.Tool;

[Trait("Category", "Unit")]
public class ContosoSampleTests
{
    [Fact]
    public void SampleSources_MatchIndependentGoldenIncludingSharingAndCycles()
    {
        var result = new TypeGenerationFrontend().Build(
            RoslynCompilationFactory.Create(ContosoSampleFixture.ReadSources()), ContosoAuthoringFixture.Metadata);
        Assert.True(result.Succeeded, string.Join(Environment.NewLine, result.Diagnostics));
        Assert.Empty(result.Diagnostics);
        Assert.True(SemanticModelComparer.AreEqual(ContosoSampleFixture.CreateExpected(), result.Model));

        var body = Assert.IsType<ObjectTypeDefinition>(Assert.Single(result.Model!.ResourceTypes).Body);
        Assert.Equal(["description", "url", "settings", "name"], body.Properties.Select(property => property.Name));
        Assert.Equal(PropertyFlags.Required | PropertyFlags.Identifier, body.Properties[3].Flags);
        Assert.Equal(PropertyFlags.ReadOnly, body.Properties[1].Flags);
        Assert.Same(body.Properties[0].Type, body.Properties[1].Type);
        var nullableSettings = Assert.IsType<UnionTypeDefinition>(body.Properties[2].Type);
        var settings = Assert.IsType<ObjectTypeDefinition>(nullableSettings.Elements[0].Type);
        Assert.Same(nullableSettings, settings.Properties[3].Type);
        var labels = Assert.IsType<ObjectTypeDefinition>(settings.Properties[1].Type);
        Assert.Same(body.Properties[0].Type, labels.AdditionalProperties);
        Assert.Same(body.Properties[3].Type, Assert.IsType<ArrayTypeDefinition>(settings.Properties[0].Type).ItemType);
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("fr-FR")]
    [InlineData("tr-TR")]
    public void TreeSnapshot_IsIndependentOfCultureAndSourcePaths(string culture)
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            var sources = ContosoSampleFixture.ReadSources().Select(source =>
                (Path.Combine(Path.GetTempPath(), "another-source-root", source.Path), source.Source)).ToArray();
            var result = new TypeGenerationFrontend().Build(
                RoslynCompilationFactory.Create(sources), ContosoAuthoringFixture.Metadata);
            Assert.True(result.Succeeded);
            var expected = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Snapshots", "Contoso.tree.txt"));
            Assert.Equal(expected.ReplaceLineEndings("\n"), EtsmTreeWriter.Render(result.Model!).ReplaceLineEndings("\n"));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DemoProjects_ReferenceOnlyTheOwnedSdk(bool negative)
    {
        var path = negative ? ContosoSampleFixture.DuplicateConfigurationProjectPath : ContosoSampleFixture.SampleProjectPath;
        var project = XDocument.Load(path);
        Assert.Equal("net10.0", Assert.Single(project.Descendants("TargetFramework")).Value);
        Assert.Empty(project.Descendants("PackageReference"));
        var reference = Assert.Single(project.Descendants("ProjectReference"));
        var referencedPath = Path.GetFullPath(reference.Attribute("Include")!.Value.Replace('\\', Path.DirectorySeparatorChar),
            Path.GetDirectoryName(path)!);
        Assert.Equal(Path.Combine(RealProjectFixture.RepositoryRoot, "src", "Azure.Deployments.Extensibility.TypeGeneration.Sdk",
            "Azure.Deployments.Extensibility.TypeGeneration.Sdk.csproj"), referencedPath);
        Assert.DoesNotContain(project.Descendants("OutputType"), element => element.Value == "Exe");
    }

    [Fact]
    public void NegativeFixture_SourceIsExcludedFromTheTestsAssembly()
    {
        Assert.Null(typeof(ContosoSampleTests).Assembly.GetType("Contoso.Invalid.FirstConfiguration"));
        Assert.Null(typeof(ContosoSampleTests).Assembly.GetType("Contoso.Invalid.SecondConfiguration"));
    }
}
