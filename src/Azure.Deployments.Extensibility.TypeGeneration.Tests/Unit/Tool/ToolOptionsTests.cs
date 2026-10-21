// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.TypeGeneration.Tool;
using Xunit;

namespace Azure.Deployments.Extensibility.TypeGeneration.Tests.Unit.Tool;

[Trait("Category", "Unit")]
public class ToolOptionsTests
{
    [Fact]
    public void Defaults_LeaveMetadataToTheProject()
    {
        var options = ToolOptions.Parse(["--project", "example.csproj"]);
        Assert.Equal(new("example.csproj"), options);
    }

    [Fact]
    public void ExplicitValues_AreRetained()
    {
        var options = ToolOptions.Parse(["--project", "a path with spaces.csproj", "--format", "tree",
            "--framework", "net10.0", "--name", "Name", "--version", "custom-version", "--singleton", "true"]);
        Assert.Equal(new("a path with spaces.csproj", "net10.0", "Name", "custom-version", true), options);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("TRUE", true)]
    public void Singleton_AcceptsOnlyBooleanText(string value, bool expected) =>
        Assert.Equal(expected, ToolOptions.Parse(["--project", "x.csproj", "--singleton", value]).Singleton);

    public static TheoryData<string[]> InvalidArguments => new()
    {
        Array.Empty<string>(),
        new[] { "--unknown", "value" },
        new[] { "--project" },
        new[] { "--project", "" },
        new[] { "--project", " " },
        new[] { "--project", "x.csproj", "--project", "y.csproj" },
        new[] { "--project", "x.csproj", "--format", "json" },
        new[] { "--project", "x.csproj", "--name", "--version", "1" },
        new[] { "--project", "x.csproj", "--singleton", "1" },
        new[] { "--project", "x.csproj", "--singleton", "yes" },
        new[] { "--project", "x.csproj", "--singleton", "false", "--singleton", "true" },
        new[] { "--project", "x.csproj", "--framework", "" },
        new[] { "--project", "x.csproj", "--version", "" },
    };

    [Theory]
    [MemberData(nameof(InvalidArguments))]
    public async Task InvalidArguments_HaveExitTwoAndNoStandardOutput(string[] args)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        Assert.Equal(2, await ToolApplication.RunAsync(args, output, error));
        Assert.Equal("", output.ToString());
        Assert.Contains("TGH0001", error.ToString());
        Assert.Contains(ToolOptions.Usage, error.ToString());
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("-h")]
    public async Task Help_DoesNotLoadMSBuild(string argument)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        Assert.Equal(0, await ToolApplication.RunAsync([argument], output, error));
        Assert.Contains(ToolOptions.Usage, output.ToString());
        Assert.Equal("", error.ToString());
    }

    [Fact]
    public async Task Cancellation_HasExit130AndNoStandardOutput()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        using var output = new StringWriter();
        using var error = new StringWriter();
        Assert.Equal(130, await ToolApplication.RunAsync(["--project", "x.csproj"], output, error, cancellation.Token));
        Assert.Equal("", output.ToString());
        Assert.Contains("TGH0002", error.ToString());
    }
}
