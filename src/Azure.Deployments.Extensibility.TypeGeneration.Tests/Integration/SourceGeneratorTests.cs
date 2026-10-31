// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Xml.Linq;
using Azure.Deployments.Extensibility.TypeGeneration.Diagnostics;
using Azure.Deployments.Extensibility.TypeGeneration.Semantics;
using Azure.Deployments.Extensibility.TypeGeneration.Tests.Support;
using Azure.Deployments.Extensibility.TypeGeneration.Tool;
using Xunit;

namespace Azure.Deployments.Extensibility.TypeGeneration.Tests.Integration;

[Trait("Category", "Integration")]
public class SourceGeneratorTests : IClassFixture<SourceGeneratorBuildFixture>
{
    public SourceGeneratorTests(SourceGeneratorBuildFixture fixture) =>
        Assert.True(File.Exists(fixture.GeneratorAssemblyPath));

    private static string ConsumerProjectPath => Path.Combine(RealProjectFixture.RepositoryRoot, "src",
        "Azure.Deployments.Extensibility.TypeGeneration.Tests", "Fixtures", "SourceGenerator", "Consumer", "Consumer.csproj");

    [Fact]
    public async Task StandardBuild_AndUnbuiltHost_SeeTheSameGeneratedPropertiesAndHelperTypes()
    {
        using var compiled = await RealProjectFixture.CopyProjectAsync(ConsumerProjectPath);
        File.WriteAllText(Path.Combine(compiled.DirectoryPath, "CompileProof.cs"), """
            namespace GeneratedModels;
            public static class CompileProof
            {
                public static int ReadCount(Repository repository) => repository.GeneratedCount;
                public static GeneratedDetails? ReadDetails(Repository repository) => repository.Details;
            }
            """);
        var build = await RealProjectFixture.RunProcessAsync(compiled.DirectoryPath,
            ["build", compiled.ProjectPath, "--configuration", "Debug", "--no-restore", "--nologo", "-v", "minimal"]);
        Assert.True(build.ExitCode == 0, build.Output + build.Error);
        Assert.DoesNotContain("CS878", build.Output);
        Assert.DoesNotContain("CS9057", build.Output);
        Assert.True(File.Exists(Path.Combine(compiled.DirectoryPath, "bin", "Debug", "net10.0", "Consumer.dll")));

        SetMode(compiled, "omit");
        var withoutGeneratedDeclarations = await RealProjectFixture.RunProcessAsync(compiled.DirectoryPath,
            ["build", compiled.ProjectPath, "--configuration", "Debug", "--no-restore", "--nologo", "-v", "minimal"]);
        Assert.Equal(1, withoutGeneratedDeclarations.ExitCode);
        Assert.Contains("CS0246", withoutGeneratedDeclarations.Output);
        Assert.Contains("GeneratedDetails", withoutGeneratedDeclarations.Output);

        using var analyzed = await RealProjectFixture.CopyProjectAsync(ConsumerProjectPath);
        AssertNoConsumerOutputs(analyzed);
        var result = await ProjectAnalyzer.AnalyzeAsync(new(analyzed.ProjectPath));
        Assert.True(result.Result.Succeeded, string.Join(Environment.NewLine, result.DiagnosticMessages));
        Assert.Empty(result.Result.Diagnostics);
        AssertExpectedModel(result.Result.Model!);
        var generatedProperty = Assert.Single(
            Assert.IsType<ObjectTypeDefinition>(result.Result.Model!.ResourceTypes[0].Body).Properties,
            property => property.Name == "generatedCount");
        Assert.EndsWith("Repository.generated.cs", generatedProperty.SourceLocation!.FilePath);

        var cli = await analyzed.RunToolAsync("--project", analyzed.ProjectPath, "--format", "tree");
        Assert.True(cli.ExitCode == 0, cli.Error);
        Assert.Equal("", cli.Error);
        Assert.Equal(EtsmTreeWriter.Render(result.Result.Model!), cli.Output);
        AssertNoConsumerOutputs(analyzed);
    }

    [Theory]
    [InlineData("throw", "CS8785")]
    [InlineData("error", "TH0801")]
    public async Task GeneratorFailure_CannotReturnASuccessfulPartialModel(string mode, string originalCode)
    {
        using var fixture = await RealProjectFixture.CopyProjectAsync(ConsumerProjectPath);
        SetMode(fixture, mode);
        var result = await ProjectAnalyzer.AnalyzeAsync(new(fixture.ProjectPath));
        Assert.False(result.Result.Succeeded);
        Assert.Null(result.Result.Model);
        Assert.Contains(result.Result.Diagnostics, diagnostic =>
            diagnostic.Severity == TypeGenDiagnosticSeverity.Error && diagnostic.Message.Contains(originalCode, StringComparison.Ordinal));
        var cli = await fixture.RunToolAsync("--project", fixture.ProjectPath);
        Assert.Equal(1, cli.ExitCode);
        Assert.Equal("", cli.Output);
        Assert.Contains(originalCode, cli.Error);
        AssertNoConsumerOutputs(fixture);
    }

    [Fact]
    public async Task GeneratorWarning_IsReportedAlongsideTheCompleteGeneratedModel()
    {
        using var fixture = await RealProjectFixture.CopyProjectAsync(ConsumerProjectPath);
        SetMode(fixture, "warning");
        var result = await ProjectAnalyzer.AnalyzeAsync(new(fixture.ProjectPath));
        Assert.True(result.Result.Succeeded, string.Join(Environment.NewLine, result.DiagnosticMessages));
        AssertExpectedModel(result.Result.Model!);
        Assert.Contains(result.Result.Diagnostics, diagnostic =>
            diagnostic.Severity == TypeGenDiagnosticSeverity.Warning && diagnostic.Message.Contains("TH0802", StringComparison.Ordinal));
        var cli = await fixture.RunToolAsync("--project", fixture.ProjectPath);
        Assert.Equal(0, cli.ExitCode);
        Assert.Contains("TH0802", cli.Error);
        Assert.Contains("generatedCount", cli.Output);
        AssertNoConsumerOutputs(fixture);
    }

    [Fact]
    public async Task InvalidGeneratedProperty_ReportsItsGeneratedSourceLocation()
    {
        using var fixture = await RealProjectFixture.CopyProjectAsync(ConsumerProjectPath);
        SetMode(fixture, "unsupported");
        var result = await ProjectAnalyzer.AnalyzeAsync(new(fixture.ProjectPath));
        Assert.False(result.Result.Succeeded);
        Assert.Null(result.Result.Model);
        var diagnostic = Assert.Single(result.Result.Diagnostics, diagnostic => diagnostic.Code == "TG1501");
        Assert.EndsWith("Repository.generated.cs", diagnostic.SourceLocation!.FilePath);
        var cli = await fixture.RunToolAsync("--project", fixture.ProjectPath);
        Assert.Equal(1, cli.ExitCode);
        Assert.Equal("", cli.Output);
        Assert.Contains("Repository.generated.cs(", cli.Error);
        Assert.Contains("TG1501", cli.Error);
        AssertNoConsumerOutputs(fixture);
    }

    [Fact]
    public async Task SingleCliAnalysis_DoesNotRunTheGeneratorTwice()
    {
        using var fixture = await RealProjectFixture.CopyProjectAsync(ConsumerProjectPath);
        SetMode(fixture, "once");
        var cli = await fixture.RunToolAsync("--project", fixture.ProjectPath);
        Assert.True(cli.ExitCode == 0, cli.Error);
        Assert.Equal("", cli.Error);
        Assert.Contains("generatedCount", cli.Output);
        Assert.Contains("GeneratedDetails", cli.Output);
        AssertNoConsumerOutputs(fixture);
    }

    [Fact]
    public async Task AdditionalFilesAndParseOptions_ReachTheGenerator()
    {
        using var fixture = await RealProjectFixture.CopyProjectAsync(ConsumerProjectPath);
        SetMode(fixture, "inputs");
        File.WriteAllText(Path.Combine(fixture.DirectoryPath, "input.txt"), "TH08 additional input");
        var project = XDocument.Load(fixture.ProjectPath);
        project.Root!.Add(new XElement("PropertyGroup", new XElement("DefineConstants", "TH08_GENERATED")));
        project.Root.Add(new XElement("ItemGroup",
            new XElement("AdditionalFiles", new XAttribute("Include", "input.txt"))));
        project.Save(fixture.ProjectPath);
        var result = await ProjectAnalyzer.AnalyzeAsync(new(fixture.ProjectPath));
        Assert.True(result.Result.Succeeded, string.Join(Environment.NewLine, result.DiagnosticMessages));
        AssertExpectedModel(result.Result.Model!);
        AssertNoConsumerOutputs(fixture);
    }

    [Fact]
    public async Task SuppressedGeneratorException_RemainsFatal()
    {
        using var fixture = await RealProjectFixture.CopyProjectAsync(ConsumerProjectPath);
        SetMode(fixture, "throw");
        AddProperty(fixture, "NoWarn", "CS8785");
        var result = await ProjectAnalyzer.AnalyzeAsync(new(fixture.ProjectPath));
        Assert.False(result.Result.Succeeded);
        Assert.Null(result.Result.Model);
        Assert.Contains(result.Result.Diagnostics, diagnostic =>
            diagnostic.Code == "TGH1009" && diagnostic.Severity == TypeGenDiagnosticSeverity.Error);
    }

    [Theory]
    [InlineData("NoWarn", "TH0802", true)]
    [InlineData("WarningsAsErrors", "TH0802", false)]
    public async Task GeneratorDiagnostics_RespectProjectSeverityOptions(string property, string value, bool succeeds)
    {
        using var fixture = await RealProjectFixture.CopyProjectAsync(ConsumerProjectPath);
        SetMode(fixture, "warning");
        AddProperty(fixture, property, value);
        var result = await ProjectAnalyzer.AnalyzeAsync(new(fixture.ProjectPath));
        Assert.Equal(succeeds, result.Result.Succeeded);
        if (succeeds)
        {
            AssertExpectedModel(result.Result.Model!);
            Assert.DoesNotContain(result.Result.Diagnostics, diagnostic => diagnostic.Message.Contains("TH0802", StringComparison.Ordinal));
        }
        else
        {
            Assert.Null(result.Result.Model);
            Assert.Contains(result.Result.Diagnostics, diagnostic =>
                diagnostic.Severity == TypeGenDiagnosticSeverity.Error && diagnostic.Message.Contains("TH0802", StringComparison.Ordinal));
        }
    }

    [Fact]
    public async Task MissingGeneratorAssembly_IsNotSilentlyIgnored()
    {
        using var fixture = await RealProjectFixture.CopyProjectAsync(ConsumerProjectPath);
        var project = XDocument.Load(fixture.ProjectPath);
        project.Root!.Add(new XElement("ItemGroup",
            new XElement("Analyzer", new XAttribute("Include", "MissingGenerator.dll"))));
        project.Save(fixture.ProjectPath);
        var result = await ProjectAnalyzer.AnalyzeAsync(new(fixture.ProjectPath));
        Assert.False(result.Result.Succeeded);
        Assert.Null(result.Result.Model);
        Assert.Contains(result.DiagnosticMessages, message => message.Contains("MissingGenerator.dll", StringComparison.Ordinal));
    }

    [Fact]
    public async Task InvalidGeneratorAssembly_ReportsTheLoadFailure()
    {
        using var fixture = await RealProjectFixture.CopyProjectAsync(ConsumerProjectPath);
        File.WriteAllText(Path.Combine(fixture.DirectoryPath, "InvalidGenerator.dll"), "Not a managed assembly.");
        var project = XDocument.Load(fixture.ProjectPath);
        project.Root!.Add(new XElement("ItemGroup",
            new XElement("Analyzer", new XAttribute("Include", "InvalidGenerator.dll"))));
        project.Save(fixture.ProjectPath);
        var result = await ProjectAnalyzer.AnalyzeAsync(new(fixture.ProjectPath));
        Assert.False(result.Result.Succeeded);
        Assert.Null(result.Result.Model);
        Assert.Contains(result.Result.Diagnostics, diagnostic =>
            diagnostic.Code == "TGH1009" && diagnostic.Message.Contains("InvalidGenerator.dll", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("success")]
    [InlineData("throw")]
    public async Task ReferencedProjectGenerators_AreIncludedAndTheirFailuresAreNotLost(string mode)
    {
        using var dependency = await RealProjectFixture.CopyProjectAsync(ConsumerProjectPath);
        SetMode(dependency, mode);
        using var fixture = await RealProjectFixture.CreateAsync(restore: false);
        var project = XDocument.Load(fixture.ProjectPath);
        project.Root!.Add(new XElement("ItemGroup",
            new XElement("ProjectReference", new XAttribute("Include", dependency.ProjectPath))));
        project.Save(fixture.ProjectPath);
        File.WriteAllText(Path.Combine(fixture.DirectoryPath, "UsesGenerated.cs"), """
            using Azure.Deployments.Extensibility.TypeGeneration.Attributes;
            [ResourceType("UsesGenerated")] public class UsesGenerated
            {
                public GeneratedModels.GeneratedDetails? Details { get; set; }
            }
            """);
        await fixture.RestoreAsync();
        var result = await ProjectAnalyzer.AnalyzeAsync(new(fixture.ProjectPath));
        if (mode == "success")
        {
            Assert.True(result.Result.Succeeded, string.Join(Environment.NewLine, result.DiagnosticMessages));
            var resource = Assert.Single(result.Result.Model!.ResourceTypes, resource => resource.Name == "UsesGenerated");
            var body = Assert.IsType<ObjectTypeDefinition>(resource.Body);
            var details = Assert.IsType<UnionTypeDefinition>(Assert.Single(body.Properties).Type);
            var generated = Assert.IsType<ObjectTypeDefinition>(details.Elements[0].Type);
            Assert.Equal("GeneratedDetails", generated.Name);
            Assert.Equal("label", Assert.Single(generated.Properties).Name);
            Assert.Equal(2, result.Result.Model.ResourceTypes.Length);
        }
        else
        {
            Assert.False(result.Result.Succeeded);
            Assert.Null(result.Result.Model);
            Assert.Contains(result.Result.Diagnostics, diagnostic =>
                diagnostic.Severity == TypeGenDiagnosticSeverity.Error && diagnostic.Message.Contains("CS8785", StringComparison.Ordinal));
        }

        AssertNoConsumerOutputs(dependency);
        AssertNoConsumerOutputs(fixture);
    }

    private static void AddProperty(RealProjectFixture fixture, string name, string value)
    {
        var project = XDocument.Load(fixture.ProjectPath);
        project.Root!.Add(new XElement("PropertyGroup", new XElement(name, value)));
        project.Save(fixture.ProjectPath);
    }

    private static void SetMode(RealProjectFixture fixture, string mode)
    {
        var project = XDocument.Load(fixture.ProjectPath);
        Assert.Single(project.Descendants("TypeGenerationTestMode")).Value = mode;
        project.Save(fixture.ProjectPath);
    }

    private static void AssertNoConsumerOutputs(RealProjectFixture fixture)
    {
        Assert.Empty(Directory.GetFiles(fixture.DirectoryPath, "*.dll", SearchOption.AllDirectories));
        Assert.Empty(Directory.GetFiles(fixture.DirectoryPath, "*.pdb", SearchOption.AllDirectories));
        Assert.Empty(Directory.GetFiles(fixture.DirectoryPath, "Repository.generated.cs", SearchOption.AllDirectories));
    }

    private static void AssertExpectedModel(ExtensionTypeSemanticModel actual)
    {
        var expected = new ExtensionTypeSemanticModel
        {
            Name = "GeneratedModels",
            Version = "1.0",
            ResourceTypes =
            [
                new()
                {
                    Name = "Repository",
                    ApiVersions = ["2026-10-01"],
                    ReadableScopes = ResourceScope.All,
                    WritableScopes = ResourceScope.All,
                    ResourceFunctions = [],
                    Body = new ObjectTypeDefinition
                    {
                        Name = "Repository",
                        Properties =
                        [
                            new()
                            {
                                Name = "name", Description = "Handwritten name",
                                Flags = PropertyFlags.Required | PropertyFlags.Identifier,
                                Type = new StringTypeDefinition(),
                            },
                            new()
                            {
                                Name = "generatedCount", Description = "Generated count", Flags = PropertyFlags.ReadOnly,
                                Type = new IntegerTypeDefinition { MinValue = int.MinValue, MaxValue = int.MaxValue },
                            },
                            new()
                            {
                                Name = "details", Description = "Generated details",
                                Type = Nullable(new ObjectTypeDefinition
                                {
                                    Name = "GeneratedDetails",
                                    Properties = [new() { Name = "label", Type = Nullable(new StringTypeDefinition()) }],
                                }),
                            },
                        ],
                    },
                },
            ],
        };
        // Partial declarations are ordered by source path, including the generator's virtual path.
        var resource = Assert.Single(actual.ResourceTypes);
        var body = Assert.IsType<ObjectTypeDefinition>(resource.Body);
        var expectedBody = (ObjectTypeDefinition)expected.ResourceTypes[0].Body;
        var canonical = actual with
        {
            ResourceTypes = [resource with { Body = body with { Properties = [.. body.Properties.OrderBy(property => property.Name, StringComparer.Ordinal)] } }],
        };
        expected = expected with
        {
            ResourceTypes = [expected.ResourceTypes[0] with
            {
                Body = expectedBody with { Properties = [.. expectedBody.Properties.OrderBy(property => property.Name, StringComparer.Ordinal)] },
            }],
        };
        Assert.True(SemanticModelComparer.AreEqual(expected, canonical));
    }

    private static UnionTypeDefinition Nullable(TypeDefinition type) =>
        new() { Elements = [new() { Type = type }, new() { Type = new NullTypeDefinition() }] };
}

public sealed class SourceGeneratorBuildFixture : IAsyncLifetime
{
    private static string GeneratorDirectory => Path.Combine(RealProjectFixture.RepositoryRoot, "src",
        "Azure.Deployments.Extensibility.TypeGeneration.Tests", "Fixtures", "SourceGenerator", "Generator");

    public string GeneratorAssemblyPath => Path.Combine(GeneratorDirectory, "bin", "Debug", "netstandard2.0", "Generator.dll");

    public async Task InitializeAsync()
    {
        // The Tool evaluates inputs as Debug even when the test runner itself is Release.
        var build = await RealProjectFixture.RunProcessAsync(RealProjectFixture.RepositoryRoot,
            ["build", Path.Combine(GeneratorDirectory, "Generator.csproj"), "--configuration", "Debug", "--no-restore", "--nologo", "-v", "minimal"]);
        Assert.True(build.ExitCode == 0, build.Output + build.Error);
    }

    public Task DisposeAsync() => Task.CompletedTask;
}
