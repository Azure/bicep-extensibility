// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.TypeGeneration.Backend;
using Azure.Deployments.Extensibility.TypeGeneration.Tests.Support;
using Azure.Deployments.Extensibility.TypeGeneration.Tool;
using Xunit;

namespace Azure.Deployments.Extensibility.TypeGeneration.Tests.Integration;

[Trait("Category", "Integration")]
public class GenerateCommandTests
{
    [Fact]
    public async Task FreshCli_GeneratesFromRelativeOutputThenSkipsIdenticalContentAndReplacesChangedContent()
    {
        using var fixture = await RealProjectFixture.CreateAsync();
        var relativeOutput = Path.Combine("artifacts with spaces", "types.tgz");
        var outputPath = Path.Combine(fixture.DirectoryPath, relativeOutput);
        var first = await fixture.RunToolAsync("generate", "--project", fixture.ProjectPath, "--output", relativeOutput);
        Assert.True(first.ExitCode == 0, first.Error);
        Assert.Equal("", first.Error);
        Assert.Contains("resourceNames=1 concreteResources=2", first.Output);
        Assert.Contains("publication=written", first.Output);
        Assert.Contains("validation=internal-json-and-archive", first.Output);
        var expected = new TypeGenerationPackageGenerator().Generate(ContosoAuthoringFixture.CreateExpected());
        Assert.True(expected.Succeeded);
        Assert.Equal<byte>(expected.Package!.ArchiveBytes, File.ReadAllBytes(outputPath));

        File.SetLastWriteTimeUtc(outputPath, new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc));
        var timestamp = File.GetLastWriteTimeUtc(outputPath);
        var second = await fixture.RunToolAsync("generate", "--project", fixture.ProjectPath, "--output", relativeOutput);
        Assert.True(second.ExitCode == 0, second.Error);
        Assert.Contains("publication=unchanged", second.Output);
        Assert.Equal(timestamp, File.GetLastWriteTimeUtc(outputPath));

        var changed = await fixture.RunToolAsync("generate", "--project", fixture.ProjectPath, "--output", relativeOutput, "--version", "1.3");
        Assert.True(changed.ExitCode == 0, changed.Error);
        Assert.Contains("publication=written", changed.Output);
        var newExpected = new TypeGenerationPackageGenerator().Generate(ContosoAuthoringFixture.CreateExpected() with { Version = "1.3" });
        Assert.Equal<byte>(newExpected.Package!.ArchiveBytes, File.ReadAllBytes(outputPath));
        Assert.Empty(Directory.GetFiles(fixture.DirectoryPath, "*.tmp", SearchOption.AllDirectories));
        Assert.Empty(Directory.GetFiles(fixture.DirectoryPath, "*.dll", SearchOption.AllDirectories));
    }

    [Theory]
    [InlineData("compiler", false)]
    [InlineData("compiler", true)]
    [InlineData("backend", false)]
    [InlineData("backend", true)]
    public async Task AnalysisOrBackendFailure_PreservesOldOutputAndNeverCreatesNewOutput(string scenario, bool existing)
    {
        using var fixture = await RealProjectFixture.CreateAsync();
        var output = Path.Combine(fixture.DirectoryPath, "output", "types.tgz");
        if (existing)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(output)!);
            File.WriteAllBytes(output, [9, 8, 7]);
        }
        var timestamp = existing ? File.GetLastWriteTimeUtc(output) : default;
        File.WriteAllText(Path.Combine(fixture.DirectoryPath, "Failure.cs"), scenario == "compiler"
            ? "public class Failure { public MissingType Value { get; set; } }"
            : """
                using Azure.Deployments.Extensibility.TypeGeneration.Attributes;
                [ResourceType("R", ApiVersions = new[] { "v1" })] public class First { public string? Name { get; set; } }
                [ResourceType("R@v1")] public class Second { public string? Name { get; set; } }
                """);
        var result = await fixture.RunToolAsync("generate", "--project", fixture.ProjectPath, "--output", output);
        Assert.Equal(1, result.ExitCode);
        Assert.Equal("", result.Output);
        Assert.Contains(scenario == "compiler" ? "TG1101" : "TG2201", result.Error);
        Assert.Contains("Failure.cs(", result.Error);
        Assert.Equal(existing, File.Exists(output));
        if (existing)
        {
            Assert.Equal<byte>([9, 8, 7], File.ReadAllBytes(output));
            Assert.Equal(timestamp, File.GetLastWriteTimeUtc(output));
        }
        else
        {
            Assert.False(Directory.Exists(Path.GetDirectoryName(output)));
        }
        Assert.Empty(Directory.GetFiles(fixture.DirectoryPath, "*.tmp", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task ProjectAndSourceCollisions_AreRejectedWithoutChangingInput()
    {
        using var fixture = await RealProjectFixture.CreateAsync();
        var projectBytes = File.ReadAllBytes(fixture.ProjectPath);
        var projectCollision = await fixture.RunToolAsync("generate", "--project", fixture.ProjectPath, "--output", fixture.ProjectPath);
        Assert.Equal(2, projectCollision.ExitCode);
        Assert.Contains("TGH1011", projectCollision.Error);
        Assert.Equal<byte>(projectBytes, File.ReadAllBytes(fixture.ProjectPath));
        var source = Directory.GetFiles(fixture.DirectoryPath, "*.cs").First();
        var sourceBytes = File.ReadAllBytes(source);
        var sourceCollision = await fixture.RunToolAsync("generate", "--project", fixture.ProjectPath, "--output", source);
        Assert.Equal(2, sourceCollision.ExitCode);
        Assert.Equal("", sourceCollision.Output);
        Assert.Contains("TGH1011", sourceCollision.Error);
        Assert.Equal<byte>(sourceBytes, File.ReadAllBytes(source));
        var directoryCollision = await fixture.RunToolAsync("generate", "--project", fixture.ProjectPath, "--output", fixture.DirectoryPath);
        Assert.Equal(2, directoryCollision.ExitCode);
        Assert.Contains("TGH1011", directoryCollision.Error);
    }

    [Fact]
    public async Task WarningsStayOnStderr_AndDoNotPreventSuccessfulPublishing()
    {
        using var fixture = await RealProjectFixture.CreateAsync();
        File.WriteAllText(Path.Combine(fixture.DirectoryPath, "Warning.cs"), "#warning Generation warning");
        var target = Path.Combine(fixture.DirectoryPath, "types.tgz");
        var result = await fixture.RunToolAsync("generate", "--project", fixture.ProjectPath, "--output", target);
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("warning TG1101", result.Error);
        Assert.DoesNotContain("warning ", result.Output);
        Assert.Contains("Type generation succeeded:", result.Output);
        Assert.True(File.Exists(target));
    }

    [Fact]
    public async Task SummaryFailure_ReportsThatPublicationAlreadyCompleted()
    {
        using var fixture = await RealProjectFixture.CreateAsync();
        var target = Path.Combine(fixture.DirectoryPath, "types.tgz");
        using var output = new FailingSummaryWriter();
        using var error = new StringWriter();
        var exit = await ToolApplication.RunAsync(["generate", "--project", fixture.ProjectPath, "--output", target], output, error);
        Assert.Equal(1, exit);
        Assert.Contains("TGH1014", error.ToString());
        Assert.Contains("publication completed", error.ToString());
        Assert.True(File.Exists(target));
        Assert.NotEmpty(File.ReadAllBytes(target));
    }

    private sealed class FailingSummaryWriter : StringWriter
    {
        public override Task WriteLineAsync(string? value) => throw new IOException("Injected summary failure.");
    }
}
