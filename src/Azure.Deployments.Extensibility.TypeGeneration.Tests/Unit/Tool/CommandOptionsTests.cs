// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.TypeGeneration.Tool;
using Xunit;

namespace Azure.Deployments.Extensibility.TypeGeneration.Tests.Unit.Tool;

[Trait("Category", "Unit")]
public class CommandOptionsTests
{
    [Fact]
    public void LegacyAndExplicitInspect_HaveTheSameDefaults()
    {
        var legacy = ToolOptions.Parse(["--project", "input.csproj"]);
        var explicitInspect = ToolOptions.Parse(["inspect", "--project", "input.csproj"]);
        Assert.Equal(legacy, explicitInspect);
        Assert.Equal(ToolCommand.Inspect, legacy.Command);
        Assert.Equal("Debug", legacy.Configuration);
        Assert.Null(legacy.OutputPath);
    }

    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    [InlineData("Profile")]
    [InlineData(" Custom ")]
    public void ConfigurationAndOverrides_ArePreserved(string configuration)
    {
        var options = ToolOptions.Parse(["generate", "--project", "a path.csproj", "--output", "an output.tgz",
            "--configuration", configuration, "--framework", "net10.0", "--name", "Name", "--version", "custom", "--singleton", "false"]);
        Assert.Equal(new("a path.csproj", "net10.0", "Name", "custom", false, configuration, ToolCommand.Generate, "an output.tgz"), options);
    }

    public static TheoryData<string[]> InvalidCommands => new()
    {
        new[] { "inspect" },
        new[] { "generate" },
        new[] { "Inspect", "--project", "x.csproj" },
        new[] { "generate", "--project", "x.csproj" },
        new[] { "generate", "--output", "x.tgz" },
        new[] { "generate", "--project", "x.csproj", "--output", "" },
        new[] { "generate", "--project", "x.csproj", "--output", "--framework", "net10.0" },
        new[] { "generate", "--project", "x.csproj", "--output", "x.tgz", "--output", "y.tgz" },
        new[] { "generate", "--project", "x.csproj", "--output", "x.tgz", "--format", "tree" },
        new[] { "inspect", "--project", "x.csproj", "--output", "x.tgz" },
        new[] { "--project", "x.csproj", "--output", "x.tgz" },
        new[] { "inspect", "--project", "x.csproj", "--configuration", "" },
        new[] { "inspect", "--project", "x.csproj", "--configuration", " " },
        new[] { "inspect", "--project", "x.csproj", "--configuration", "Debug", "--configuration", "Release" },
        new[] { "inspect", "--project", "x.csproj", "--configuration", "--framework", "net10.0" },
    };

    [Theory]
    [MemberData(nameof(InvalidCommands))]
    public async Task InvalidCommandArguments_FailWithoutLoadingAProject(string[] args)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        Assert.Equal(2, await ToolApplication.RunAsync(args, output, error));
        Assert.Equal("", output.ToString());
        Assert.Contains("TGH0001", error.ToString());
        Assert.Contains(ToolOptions.Usage, error.ToString());
    }

    [Theory]
    [InlineData("inspect", "--help")]
    [InlineData("inspect", "-h")]
    [InlineData("generate", "--help")]
    [InlineData("generate", "-h")]
    public async Task CommandHelp_HasNoProjectSideEffects(string command, string help)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        Assert.Equal(0, await ToolApplication.RunAsync([command, help], output, error));
        Assert.Equal((command == "inspect" ? ToolOptions.InspectUsage : ToolOptions.GenerateUsage) + Environment.NewLine, output.ToString());
        Assert.Equal("", error.ToString());
    }

    [Fact]
    public async Task Generate_MissingProjectFailsWithoutCreatingOutput()
    {
        var directory = Path.Combine(Path.GetTempPath(), "TypeGenerationCommandTests", Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "types.tgz");
        using var output = new StringWriter();
        using var error = new StringWriter();
        Assert.Equal(1, await ToolApplication.RunAsync(["generate", "--project", "missing.csproj", "--output", path], output, error));
        Assert.Equal("", output.ToString());
        Assert.Contains("TGH1002", error.ToString());
        Assert.False(File.Exists(path));
        Assert.False(Directory.Exists(directory));
    }

    [Theory]
    [InlineData("inspect")]
    [InlineData("generate")]
    public async Task Cancellation_StillPrecedesCommandExecution(string command)
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        using var output = new StringWriter();
        using var error = new StringWriter();
        Assert.Equal(130, await ToolApplication.RunAsync([command, "--help"], output, error, cancellation.Token));
        Assert.Equal("", output.ToString());
        Assert.Contains("TGH0002", error.ToString());
    }
}
