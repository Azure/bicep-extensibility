// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml.Linq;
using Azure.Deployments.Extensibility.TypeGeneration.Backend;
using Azure.Deployments.Extensibility.TypeGeneration.Diagnostics;
using Azure.Deployments.Extensibility.TypeGeneration.Semantics;
using Azure.Deployments.Extensibility.TypeGeneration.Tests.Support;
using Azure.Deployments.Extensibility.TypeGeneration.Tool;
using Xunit;
using Xunit.Abstractions;

namespace Azure.Deployments.Extensibility.TypeGeneration.Tests.Integration;

[Trait("Category", "Integration")]
public class HostCommandTests(ITestOutputHelper output)
{
    private const string ConfigurationProperties = """
        <BicepExtensionName>$(Configuration)Name</BicepExtensionName>
        <BicepExtensionVersion>$(Configuration)-version</BicepExtensionVersion>
        <BicepExtensionIsSingleton Condition="'$(Configuration)' == 'Release'">true</BicepExtensionIsSingleton>
        <DefineConstants Condition="'$(Configuration)' == 'Release'">$(DefineConstants);SELECTED_RELEASE</DefineConstants>
        """;

    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    [InlineData("Profile")]
    public async Task Configuration_DrivesMetadataCompilationAndExplicitInspect(string configuration)
    {
        using var fixture = await RealProjectFixture.CreateAsync(ConfigurationProperties);
        File.WriteAllText(Path.Combine(fixture.DirectoryPath, "Selected.cs"), """
            #if SELECTED_RELEASE
            using Azure.Deployments.Extensibility.TypeGeneration.Attributes;
            [ResourceType("ReleaseOnly")] public class ReleaseOnly { public bool Enabled { get; set; } }
            #endif
            """);
        var analysis = await ProjectAnalyzer.AnalyzeAsync(new(fixture.ProjectPath, Configuration: configuration));
        Assert.True(analysis.Result.Succeeded, string.Join(Environment.NewLine, analysis.DiagnosticMessages));
        Assert.Equal(configuration + "Name", analysis.Result.Model!.Name);
        Assert.Equal(configuration + "-version", analysis.Result.Model.Version);
        Assert.Equal(configuration == "Release", analysis.Result.Model.IsSingleton);
        Assert.Equal(configuration == "Release", analysis.Result.Model.ResourceTypes.Any(resource => resource.Name == "ReleaseOnly"));
        var cli = await fixture.RunToolAsync("inspect", "--project", fixture.ProjectPath, "--configuration", configuration);
        Assert.True(cli.ExitCode == 0, cli.Error);
        Assert.Equal(EtsmTreeWriter.Render(analysis.Result.Model), cli.Output);
        Assert.Equal("", cli.Error);
        Assert.Empty(Directory.GetFiles(fixture.DirectoryPath, "*.dll", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task ConfigurationAndFramework_SelectTheSameRootWithoutOverridingReferenceFrameworks()
    {
        using var fixture = await RealProjectFixture.CreateAsync(ConfigurationProperties, multiTarget: true);
        File.WriteAllText(Path.Combine(fixture.DirectoryPath, "Selected.cs"), """
            using Azure.Deployments.Extensibility.TypeGeneration.Attributes;
            #if WINDOWS && SELECTED_RELEASE
            [ResourceType("Selected")] public class Selected { public bool Enabled { get; set; } }
            #endif
            """);
        var result = await ProjectAnalyzer.AnalyzeAsync(new(fixture.ProjectPath, "net10.0-windows", Configuration: "Release"));
        Assert.True(result.Result.Succeeded, string.Join(Environment.NewLine, result.DiagnosticMessages));
        Assert.Equal("ReleaseName", result.Result.Model!.Name);
        Assert.Contains(result.Result.Model.ResourceTypes, resource => resource.Name == "Selected");
        var overridden = await ProjectAnalyzer.AnalyzeAsync(new(fixture.ProjectPath, "net10.0", "Override", "custom", false, "Release"));
        Assert.True(overridden.Result.Succeeded, string.Join(Environment.NewLine, overridden.DiagnosticMessages));
        Assert.Equal("Override", overridden.Result.Model!.Name);
        Assert.Equal("custom", overridden.Result.Model.Version);
        Assert.False(overridden.Result.Model.IsSingleton);
        Assert.DoesNotContain(overridden.Result.Model.ResourceTypes, resource => resource.Name == "Selected");
        var ambiguous = await Assert.ThrowsAsync<ToolException>(() => ProjectAnalyzer.AnalyzeAsync(new(fixture.ProjectPath, Configuration: "Release")));
        Assert.Equal("TGH1003", ambiguous.Code);
    }

    [Fact]
    public async Task LegacyAndExplicitInspect_KeepIdenticalDefaultOutput()
    {
        using var fixture = await RealProjectFixture.CreateAsync();
        var legacy = await fixture.RunToolAsync("--project", fixture.ProjectPath, "--format", "tree");
        var explicitInspect = await fixture.RunToolAsync("inspect", "--project", fixture.ProjectPath, "--format", "tree", "--configuration", "Debug");
        Assert.True(legacy.ExitCode == 0, legacy.Error);
        Assert.Equal(legacy.ExitCode, explicitInspect.ExitCode);
        Assert.Equal(legacy.Output, explicitInspect.Output);
        Assert.Equal(legacy.Error, explicitInspect.Error);
        Assert.Empty(Directory.GetFiles(fixture.DirectoryPath, "*.tgz", SearchOption.AllDirectories));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task DirectHostCalls_RejectBlankConfigurationInsteadOfFallingBack(string? configuration)
    {
        using var fixture = await RealProjectFixture.CreateAsync(restore: false);
        var error = await Assert.ThrowsAsync<ToolException>(() =>
            ProjectAnalyzer.AnalyzeAsync(new(fixture.ProjectPath, Configuration: configuration!)));
        Assert.Equal("TGH1010", error.Code);
        Assert.Contains(fixture.ProjectPath, error.Message);
        Assert.False(File.Exists(Path.Combine(fixture.DirectoryPath, "obj", "project.assets.json")));
    }

    [Fact]
    public async Task BackendDiagnostics_UseCapturedSourceAfterWorkspaceDisposalAndFileChanges()
    {
        using var fixture = await RealProjectFixture.CreateAsync();
        var analysis = await ProjectAnalyzer.AnalyzeAsync(new(fixture.ProjectPath));
        Assert.True(analysis.Result.Succeeded, string.Join(Environment.NewLine, analysis.DiagnosticMessages));
        var model = analysis.Result.Model!;
        var resource = Assert.Single(model.ResourceTypes);
        var body = Assert.IsType<ObjectTypeDefinition>(resource.Body);
        var property = Assert.Single(body.Properties, property => property.Name == "name");
        var location = property.SourceLocation!;
        var originalText = File.ReadAllText(location.FilePath);
        var prefix = originalText[..location.Start];
        var line = prefix.Count(character => character == '\n') + 1;
        var column = location.Start - prefix.LastIndexOf('\n');
        var invalid = model with { ResourceTypes = [resource with { Body = body with { Properties = [property with { Flags = (PropertyFlags)32 }] } }] };
        var backend = new TypeGenerationPackageGenerator().Generate(invalid);
        Assert.False(backend.Succeeded);
        var error = Assert.Single(backend.Diagnostics);
        Assert.Equal("TG2008", error.Code);
        File.WriteAllText(location.FilePath, "");
        var formatted = analysis.DiagnosticFormatter.Format(error);
        Assert.StartsWith($"{location.FilePath}({line},{column}): error TG2008:", formatted, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReleaseGeneratorsAndReferencedVirtualSources_UseTheSelectedConfigurationOnce()
    {
        output.WriteLine("Building the Release generator.");
        var generatorPath = Path.Combine(RealProjectFixture.RepositoryRoot, "src", "Azure.Deployments.Extensibility.TypeGeneration.Tests",
            "Fixtures", "SourceGenerator", "Generator", "Generator.csproj");
        var build = await RealProjectFixture.RunProcessAsync(RealProjectFixture.RepositoryRoot,
            ["build", generatorPath, "--configuration", "Release", "--no-restore", "--nologo", "-v", "minimal"]);
        Assert.True(build.ExitCode == 0, build.Output + build.Error);
        output.WriteLine("Release generator build completed; preparing the dependency.");
        var consumerPath = Path.Combine(RealProjectFixture.RepositoryRoot, "src", "Azure.Deployments.Extensibility.TypeGeneration.Tests",
            "Fixtures", "SourceGenerator", "Consumer", "Consumer.csproj");
        using var dependency = await RealProjectFixture.CopyProjectAsync(consumerPath);
        output.WriteLine("Dependency restored; preparing the root.");
        var project = XDocument.Load(dependency.ProjectPath);
        project.Root!.Add(new XElement("PropertyGroup", new XAttribute("Condition", "'$(Configuration)' == 'Release'"),
            new XElement("TypeGenerationTestMode", "inputs"),
            new XElement("DefineConstants", "$(DefineConstants);TH08_GENERATED")));
        project.Root.Add(new XElement("ItemGroup", new XAttribute("Condition", "'$(Configuration)' == 'Release'"),
            new XElement("AdditionalFiles", new XAttribute("Include", "release-input.txt"))));
        project.Save(dependency.ProjectPath);
        File.WriteAllText(Path.Combine(dependency.DirectoryPath, "release-input.txt"), "TH08 additional input");

        using var root = await RealProjectFixture.CreateAsync(ConfigurationProperties, restore: false);
        var rootProject = XDocument.Load(root.ProjectPath);
        rootProject.Root!.Add(new XElement("ItemGroup", new XElement("ProjectReference", new XAttribute("Include", dependency.ProjectPath))));
        rootProject.Save(root.ProjectPath);
        File.WriteAllText(Path.Combine(root.DirectoryPath, "UsesGenerated.cs"), """
            using Azure.Deployments.Extensibility.TypeGeneration.Attributes;
            [ResourceType("UsesGenerated")] public class UsesGenerated
            {
                public GeneratedModels.GeneratedDetails? Details { get; set; }
            }
            """);
        await root.RestoreAsync();
        output.WriteLine("Root restored; analyzing the Release graph.");
        var analysis = await ProjectAnalyzer.AnalyzeAsync(new(root.ProjectPath, Configuration: "Release"));
        output.WriteLine("Release graph analyzed; checking the retained generated source.");
        Assert.True(analysis.Result.Succeeded, string.Join(Environment.NewLine, analysis.DiagnosticMessages));
        Assert.Equal("ReleaseName", analysis.Result.Model!.Name);
        var resource = Assert.Single(analysis.Result.Model.ResourceTypes, resource => resource.Name == "UsesGenerated");
        var body = Assert.IsType<ObjectTypeDefinition>(resource.Body);
        var nullable = Assert.IsType<UnionTypeDefinition>(Assert.Single(body.Properties).Type);
        var details = Assert.IsType<ObjectTypeDefinition>(nullable.Elements[0].Type);
        var label = Assert.Single(details.Properties);
        Assert.Equal("label", label.Name);
        Assert.EndsWith("Repository.generated.cs", label.SourceLocation!.FilePath);
        Assert.False(File.Exists(label.SourceLocation.FilePath));
        var formatted = analysis.DiagnosticFormatter.Format(new("TG2405", TypeGenDiagnosticSeverity.Error, "Backend generated location.", label.SourceLocation));
        Assert.Contains("Repository.generated.cs(", formatted, StringComparison.Ordinal);
        Assert.EndsWith(": error TG2405: Backend generated location.", formatted, StringComparison.Ordinal);
        Assert.DoesNotContain("[", formatted, StringComparison.Ordinal);

        project.Descendants("TypeGenerationTestMode").Last().Value = "once";
        project.Save(dependency.ProjectPath);
        output.WriteLine("Running the once-only Release generator in a fresh CLI process.");
        var cli = await dependency.RunToolAsync("inspect", "--project", dependency.ProjectPath, "--configuration", "Release");
        output.WriteLine("Fresh CLI process completed.");
        Assert.True(cli.ExitCode == 0, cli.Error);
        Assert.Contains("generatedCount", cli.Output, StringComparison.Ordinal);
        Assert.Empty(Directory.GetFiles(root.DirectoryPath, "*.dll", SearchOption.AllDirectories));
        Assert.Empty(Directory.GetFiles(dependency.DirectoryPath, "*.dll", SearchOption.AllDirectories));
        Assert.Empty(Directory.GetFiles(dependency.DirectoryPath, "Repository.generated.cs", SearchOption.AllDirectories));
    }
}
