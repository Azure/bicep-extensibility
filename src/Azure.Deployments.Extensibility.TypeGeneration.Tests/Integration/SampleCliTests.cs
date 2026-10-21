// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Security.Cryptography;
using System.Xml.Linq;
using Azure.Deployments.Extensibility.TypeGeneration.Diagnostics;
using Azure.Deployments.Extensibility.TypeGeneration.Semantics;
using Azure.Deployments.Extensibility.TypeGeneration.Tests.Support;
using Azure.Deployments.Extensibility.TypeGeneration.Tool;
using Xunit;

namespace Azure.Deployments.Extensibility.TypeGeneration.Tests.Integration;

[Trait("Category", "Integration")]
public class SampleCliTests
{
    [Fact]
    public async Task OfficialSample_HasGoldenModelStableCliAndOnlyDesignTimeFiles()
    {
        using var fixture = await RealProjectFixture.CopyProjectAsync(ContosoSampleFixture.SampleProjectPath);
        var before = CaptureFiles(fixture.DirectoryPath);
        var analysis = await AnalyzeAsync(fixture);
        Assert.True(SemanticModelComparer.AreEqual(ContosoSampleFixture.CreateExpected(), analysis.Result.Model));
        var first = await RunSuccessfulCliAsync(fixture);
        Assert.Equal(ReadSnapshot("Contoso.tree.txt"), NormalizeLines(first.Output));
        Assert.Equal(NormalizeLines(EtsmTreeWriter.Render(analysis.Result.Model!)), NormalizeLines(first.Output));
        var second = await RunSuccessfulCliAsync(fixture);
        Assert.Equal(first.Output, second.Output);
        Assert.Equal(1, first.Output.Split("  resource \"Repository\"").Length - 1);
        Assert.Contains("versions=[\"2026-08-01\", \"2026-09-01\"]", first.Output);
        Assert.Contains("property \"parent\" flags=None description=\"Parent settings\": ref #6", first.Output);
        AssertOnlyDesignTimeFiles(fixture, before);
    }

    [Fact]
    public async Task SampleCopies_WithReversedCompileOrder_HaveIdenticalModelsAndOutput()
    {
        using var first = await RealProjectFixture.CopyProjectAsync(ContosoSampleFixture.SampleProjectPath);
        using var second = await RealProjectFixture.CopyProjectAsync(ContosoSampleFixture.SampleProjectPath);
        var project = XDocument.Load(second.ProjectPath);
        project.Root!.Add(new XElement("PropertyGroup", new XElement("EnableDefaultCompileItems", "false")));
        project.Root.Add(new XElement("ItemGroup",
            Directory.GetFiles(second.DirectoryPath, "*.cs").OrderDescending(StringComparer.Ordinal)
                .Select(path => new XElement("Compile", new XAttribute("Include", Path.GetFileName(path))))));
        project.Save(second.ProjectPath);

        var firstModel = (await AnalyzeAsync(first)).Result.Model;
        var secondModel = (await AnalyzeAsync(second)).Result.Model;
        Assert.True(SemanticModelComparer.AreEqual(firstModel, secondModel));
        var firstOutput = await RunSuccessfulCliAsync(first);
        var secondOutput = await RunSuccessfulCliAsync(second);
        Assert.Equal(firstOutput.Output, secondOutput.Output);
        Assert.Equal(ReadSnapshot("Contoso.tree.txt"), NormalizeLines(secondOutput.Output));
    }

    [Fact]
    public async Task AddingSupportedProperty_ChangesTheCompleteModelAndCli()
    {
        using var fixture = await RealProjectFixture.CopyProjectAsync(ContosoSampleFixture.SampleProjectPath);
        var original = (await AnalyzeAsync(fixture)).Result.Model!;
        var originalOutput = await RunSuccessfulCliAsync(fixture);
        var path = Path.Combine(fixture.DirectoryPath, "Repository.cs");
        var source = File.ReadAllText(path);
        const string marker = "    [TypeProperty(\"Repository settings\")]";
        Assert.Equal(1, source.Split(marker).Length - 1);
        File.WriteAllText(path, source.Replace(marker,
            "    [TypeProperty(\"Archived\")]\n    public bool Archived { get; set; }\n\n" + marker, StringComparison.Ordinal));
        var before = CaptureFiles(fixture.DirectoryPath);

        var changed = (await AnalyzeAsync(fixture)).Result.Model!;
        var expected = ContosoSampleFixture.CreateExpected();
        var resource = expected.ResourceTypes[0];
        var body = (ObjectTypeDefinition)resource.Body;
        expected = expected with
        {
            ResourceTypes =
            [
                resource with
                {
                    Body = body with
                    {
                        Properties = body.Properties.Insert(2,
                            new() { Name = "archived", Description = "Archived", Type = new BooleanTypeDefinition() }),
                    },
                },
            ],
        };
        Assert.True(SemanticModelComparer.AreEqual(expected, changed));
        Assert.False(SemanticModelComparer.AreEqual(original, changed));
        var output = await RunSuccessfulCliAsync(fixture);
        Assert.NotEqual(originalOutput.Output, output.Output);
        Assert.Contains("property \"archived\" flags=None description=\"Archived\":", output.Output);
        Assert.Equal(NormalizeLines(EtsmTreeWriter.Render(changed)), NormalizeLines(output.Output));
        AssertNoNewFilesOrInputChanges(fixture, before);
    }

    [Fact]
    public Task ChangingOnlyVersions_PreservesBodyAndChangesOnlyTheLabelsInTheTree() =>
        AssertVersionChangeAsync(["preview", "stable"]);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public Task AddingOrRemovingVersionLabel_PreservesBodyAndSingleResource(bool addVersion) =>
        AssertVersionChangeAsync(addVersion
            ? ["2026-08-01", "2026-09-01", "2026-10-01"]
            : ["2026-08-01"]);

    [Fact]
    public async Task DuplicateConfiguration_HasExactDiagnosticsSpansAndNoSuccessOutput()
    {
        using var fixture = await RealProjectFixture.CopyProjectAsync(ContosoSampleFixture.DuplicateConfigurationProjectPath);
        var before = CaptureFiles(fixture.DirectoryPath);
        var analysis = await ProjectAnalyzer.AnalyzeAsync(new(fixture.ProjectPath));
        Assert.False(analysis.Result.Succeeded);
        Assert.Null(analysis.Result.Model);
        Assert.Equal(2, analysis.Result.Diagnostics.Length);
        var sourcePath = Path.Combine(fixture.DirectoryPath, "Configuration.cs");
        var source = File.ReadAllText(sourcePath);
        var starts = new[] { source.IndexOf("BicepConfiguration", StringComparison.Ordinal), source.LastIndexOf("BicepConfiguration", StringComparison.Ordinal) };
        for (var index = 0; index < starts.Length; index++)
        {
            var diagnostic = analysis.Result.Diagnostics[index];
            Assert.Equal("TG1205", diagnostic.Code);
            Assert.Equal(TypeGenDiagnosticSeverity.Error, diagnostic.Severity);
            Assert.Equal(new SourceLocation { FilePath = sourcePath, Start = starts[index], Length = "BicepConfiguration".Length },
                diagnostic.SourceLocation);
        }

        var output = await fixture.RunToolAsync("--project", fixture.ProjectPath, "--format", "tree");
        Assert.Equal(1, output.ExitCode);
        Assert.Equal("", output.Output);
        Assert.Equal(ReadSnapshot("DuplicateConfiguration.stderr.txt"), NormalizeDiagnostics(output.Error, fixture.DirectoryPath));
        Assert.Equal(NormalizeLines(string.Join(Environment.NewLine, analysis.DiagnosticMessages) + Environment.NewLine),
            NormalizeLines(output.Error));
        AssertOnlyDesignTimeFiles(fixture, before);
    }

    [Fact]
    public async Task DuplicateConfiguration_InAnotherDirectory_KeepsTheNormalizedSnapshot()
    {
        using var fixture = await RealProjectFixture.CopyProjectAsync(ContosoSampleFixture.DuplicateConfigurationProjectPath);
        var output = await fixture.RunToolAsync("--project", fixture.ProjectPath);
        Assert.Equal(1, output.ExitCode);
        Assert.Equal("", output.Output);
        Assert.Equal(ReadSnapshot("DuplicateConfiguration.stderr.txt"), NormalizeDiagnostics(output.Error, fixture.DirectoryPath));
    }

    private static async Task AssertVersionChangeAsync(string[] versions)
    {
        using var fixture = await RealProjectFixture.CopyProjectAsync(ContosoSampleFixture.SampleProjectPath);
        var original = (await AnalyzeAsync(fixture)).Result.Model!;
        var originalOutput = await RunSuccessfulCliAsync(fixture);
        var path = Path.Combine(fixture.DirectoryPath, "Repository.cs");
        var source = File.ReadAllText(path);
        const string originalLabels = "\"2026-08-01\", \"2026-09-01\"";
        var replacementLabels = string.Join(", ", versions.Select(version => $"\"{version}\""));
        Assert.Contains(originalLabels, source);
        File.WriteAllText(path, source.Replace(originalLabels, replacementLabels, StringComparison.Ordinal));
        var before = CaptureFiles(fixture.DirectoryPath);
        var changed = (await AnalyzeAsync(fixture)).Result.Model!;
        var originalResource = Assert.Single(original.ResourceTypes);
        var changedResource = Assert.Single(changed.ResourceTypes);
        Assert.Equal(versions, changedResource.ApiVersions.ToArray());
        Assert.True(SemanticModelComparer.AreEqual(originalResource.Body, changedResource.Body));
        Assert.True(SemanticModelComparer.AreEqual(
            original with { ResourceTypes = [originalResource with { ApiVersions = [.. versions] }] }, changed));
        var output = await RunSuccessfulCliAsync(fixture);
        Assert.Equal(originalOutput.Output.Replace($"[{originalLabels}]", $"[{replacementLabels}]", StringComparison.Ordinal), output.Output);
        AssertNoNewFilesOrInputChanges(fixture, before);
    }

    private static async Task<ProjectAnalysis> AnalyzeAsync(RealProjectFixture fixture)
    {
        var analysis = await ProjectAnalyzer.AnalyzeAsync(new(fixture.ProjectPath));
        Assert.True(analysis.Result.Succeeded, string.Join(Environment.NewLine, analysis.DiagnosticMessages));
        Assert.Empty(analysis.Result.Diagnostics);
        return analysis;
    }

    private static async Task<ProcessResult> RunSuccessfulCliAsync(RealProjectFixture fixture)
    {
        var output = await fixture.RunToolAsync("--project", fixture.ProjectPath, "--format", "tree");
        Assert.True(output.ExitCode == 0, output.Error);
        Assert.Equal("", output.Error);
        return output;
    }

    private static string ReadSnapshot(string name) =>
        NormalizeLines(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Snapshots", name)));

    private static string NormalizeLines(string text) => text.ReplaceLineEndings("\n");

    private static string NormalizeDiagnostics(string text, string projectDirectory) =>
        NormalizeLines(text.Replace(projectDirectory + Path.DirectorySeparatorChar, "<project>/", StringComparison.Ordinal));

    private static Dictionary<string, string> CaptureFiles(string directory) =>
        Directory.GetFiles(directory, "*", SearchOption.AllDirectories).ToDictionary(
            path => Path.GetRelativePath(directory, path),
            path => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))), StringComparer.Ordinal);

    private static void AssertOnlyDesignTimeFiles(RealProjectFixture fixture, Dictionary<string, string> before)
    {
        var after = CaptureFiles(fixture.DirectoryPath);
        foreach (var (path, hash) in before)
        {
            Assert.True(after.TryGetValue(path, out var afterHash), $"Input file disappeared: {path}");
            Assert.Equal(hash, afterHash);
        }

        var name = Path.GetFileNameWithoutExtension(fixture.ProjectPath);
        string[] generatedNames =
        [
            ".NETCoreApp,Version=v10.0.AssemblyAttributes.cs",
            $"{name}.AssemblyInfo.cs",
            $"{name}.AssemblyInfoInputs.cache",
            $"{name}.assets.cache",
            $"{name}.csproj.AssemblyReference.cache",
            $"{name}.GeneratedMSBuildEditorConfig.editorconfig",
        ];
        Assert.Equal(
            generatedNames.Select(name => Path.Combine("obj", "Debug", "net10.0", name)).Order(StringComparer.Ordinal),
            after.Keys.Except(before.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal));
        AssertNoRuntimeOrCanonicalFiles(after.Keys);
    }

    private static void AssertNoNewFilesOrInputChanges(RealProjectFixture fixture, Dictionary<string, string> before)
    {
        var after = CaptureFiles(fixture.DirectoryPath);
        Assert.Equal(before.OrderBy(pair => pair.Key, StringComparer.Ordinal), after.OrderBy(pair => pair.Key, StringComparer.Ordinal));
        AssertNoRuntimeOrCanonicalFiles(after.Keys);
    }

    private static void AssertNoRuntimeOrCanonicalFiles(IEnumerable<string> paths)
    {
        Assert.DoesNotContain(paths, path => Path.GetExtension(path) is ".dll" or ".pdb" ||
            Path.GetFileName(path) is "index.json" or "types.json");
    }
}
