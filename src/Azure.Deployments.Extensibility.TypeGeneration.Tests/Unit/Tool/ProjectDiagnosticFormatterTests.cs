// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.TypeGeneration.Diagnostics;
using Azure.Deployments.Extensibility.TypeGeneration.Semantics;
using Azure.Deployments.Extensibility.TypeGeneration.Tests.Support;
using Azure.Deployments.Extensibility.TypeGeneration.Tool;
using Xunit;

namespace Azure.Deployments.Extensibility.TypeGeneration.Tests.Unit.Tool;

[Trait("Category", "Unit")]
public class ProjectDiagnosticFormatterTests
{
    [Fact]
    public async Task VirtualSource_UsesPhysicalUtf16PositionsWithoutMappedLineDirectives()
    {
        var formatter = await ProjectDiagnosticFormatter.CreateAsync(RoslynCompilationFactory.Create("// \U0001F600x\n", "virtual.cs"));
        Assert.Equal("virtual.cs(1,6): error TG2201: Backend.", formatter.Format(Diagnostic("virtual.cs", 5, 1)));
        var mapped = await ProjectDiagnosticFormatter.CreateAsync(RoslynCompilationFactory.Create("#line 900 \"elsewhere.cs\"\nclass C {}", "physical.cs"));
        Assert.Equal("physical.cs(2,7): error TG2201: Backend.", mapped.Format(Diagnostic("physical.cs", "#line 900 \"elsewhere.cs\"\nclass ".Length, 1)));
        Assert.False(File.Exists("virtual.cs"));
    }

    [Fact]
    public async Task TransitiveCompilationReferenceSources_AreCaptured()
    {
        var leaf = RoslynCompilationFactory.Create("// generated\nclass C {}", "virtual.generated.cs").WithAssemblyName("Leaf");
        var middle = RoslynCompilationFactory.Create("class Middle {}", "middle.cs").WithAssemblyName("Middle").AddReferences(leaf.ToMetadataReference());
        var root = RoslynCompilationFactory.Create("class Root {}", "root.cs").WithAssemblyName("Root").AddReferences(middle.ToMetadataReference());
        var formatter = await ProjectDiagnosticFormatter.CreateAsync(root);
        Assert.Equal("virtual.generated.cs(2,7): error TG2201: Backend.", formatter.Format(Diagnostic("virtual.generated.cs", "// generated\nclass ".Length, 1)));
    }

    [Theory]
    [InlineData("missing.cs", 0, 1)]
    [InlineData("known.cs", -1, 1)]
    [InlineData("known.cs", 0, -1)]
    [InlineData("known.cs", 100, 1)]
    [InlineData("known.cs", 1, 100)]
    [InlineData("known.cs", int.MaxValue, int.MaxValue)]
    public async Task UnknownOrInvalidSpans_FallBackWithoutThrowing(string path, int start, int length)
    {
        var formatter = await ProjectDiagnosticFormatter.CreateAsync(RoslynCompilationFactory.Create("// x", "known.cs"));
        Assert.Equal($"{path}[{start}+{length}]: error TG2201: Backend.", formatter.Format(Diagnostic(path, start, length)));
    }

    [Fact]
    public async Task EmptyAndEndOfFileLocations_AreValid()
    {
        var empty = await ProjectDiagnosticFormatter.CreateAsync(RoslynCompilationFactory.Create("", "empty.cs"));
        Assert.Equal("empty.cs(1,1): error TG2201: Backend.", empty.Format(Diagnostic("empty.cs", 0, 0)));
        var formatter = await ProjectDiagnosticFormatter.CreateAsync(RoslynCompilationFactory.Create("//x\n", "end.cs"));
        Assert.Equal("end.cs(2,1): error TG2201: Backend.", formatter.Format(Diagnostic("end.cs", 4, 0)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConflictingPaths_DoNotPickAnArbitraryCompilation(bool reverse)
    {
        var first = RoslynCompilationFactory.Create("// first\nclass A {}", "shared.cs").WithAssemblyName("First");
        var second = RoslynCompilationFactory.Create("//different\n\nclass B {}", "shared.cs").WithAssemblyName("Second");
        var root = RoslynCompilationFactory.Create("class Root {}", "root.cs").WithAssemblyName("Root")
            .AddReferences(reverse ? [second.ToMetadataReference(), first.ToMetadataReference()] : [first.ToMetadataReference(), second.ToMetadataReference()]);
        var formatter = await ProjectDiagnosticFormatter.CreateAsync(root);
        Assert.Equal("shared.cs[3+1]: error TG2201: Backend.", formatter.Format(Diagnostic("shared.cs", 3, 1)));
    }

    [Fact]
    public async Task IdenticalTextAtSharedPaths_RemainsUsableAndOldSnapshotsDoNotChange()
    {
        var source = "// same\nclass C {}";
        var first = RoslynCompilationFactory.Create(source, "shared.cs").WithAssemblyName("First");
        var second = RoslynCompilationFactory.Create(source, "shared.cs").WithAssemblyName("Second");
        var root = first.AddReferences(second.ToMetadataReference());
        var formatter = await ProjectDiagnosticFormatter.CreateAsync(root);
        root = root.RemoveAllSyntaxTrees();
        Assert.Empty(root.SyntaxTrees);
        Assert.Equal("shared.cs(2,7): error TG2201: Backend.", formatter.Format(Diagnostic("shared.cs", "// same\nclass ".Length, 1)));
    }

    [Fact]
    public async Task BatchFormatting_PreservesOrderAndLocationlessDiagnostics()
    {
        var formatter = await ProjectDiagnosticFormatter.CreateAsync(RoslynCompilationFactory.Create("// x", "known.cs"));
        TypeGenDiagnostic[] diagnostics =
        [
            new("TG2401", TypeGenDiagnosticSeverity.Warning, "Warning.", null),
            Diagnostic("known.cs", 3, 1),
            new("TG2402", TypeGenDiagnosticSeverity.Info, "Information.", null),
        ];
        Assert.Equal<string>(
            ["warning TG2401: Warning.", "known.cs(1,4): error TG2201: Backend.", "info TG2402: Information."],
            formatter.Format(diagnostics));
    }

    [Fact]
    public async Task NullArgumentsAndCancellation_AreExplicit()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => ProjectDiagnosticFormatter.CreateAsync(null!));
        var compilation = RoslynCompilationFactory.Create("");
        var formatter = await ProjectDiagnosticFormatter.CreateAsync(compilation);
        Assert.Throws<ArgumentNullException>(() => formatter.Format((TypeGenDiagnostic)null!));
        Assert.Throws<ArgumentNullException>(() => formatter.Format((IEnumerable<TypeGenDiagnostic>)null!));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => ProjectDiagnosticFormatter.CreateAsync(compilation, cancellation.Token));
        Assert.Throws<OperationCanceledException>(() => formatter.Format([], cancellation.Token));
    }

    private static TypeGenDiagnostic Diagnostic(string path, int start, int length) =>
        new("TG2201", TypeGenDiagnosticSeverity.Error, "Backend.", new SourceLocation { FilePath = path, Start = start, Length = length });
}
