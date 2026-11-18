// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using Azure.Deployments.Extensibility.TypeGeneration.Tool;
using Xunit;

namespace Azure.Deployments.Extensibility.TypeGeneration.Tests.Unit.Tool;

[Trait("Category", "Unit")]
public class PackagePublisherTests
{
    [Fact]
    public async Task NewFile_CreatesParentsAndPublishesTheCompleteBytes()
    {
        using var directory = new OutputDirectory();
        var path = Path.Combine(directory.Path, "nested output", "types.tgz");
        var result = await new TypeGenerationPackagePublisher().PublishAsync(path, [1, 2, 3]);
        Assert.True(result.Changed);
        Assert.Equal(Path.GetFullPath(path), result.OutputPath);
        Assert.Equal<byte>([1, 2, 3], File.ReadAllBytes(path));
        Assert.Empty(Directory.GetFiles(directory.Path, "*.tmp", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task EqualContent_DoesNotOpenAStagingFileOrChangeTimestamp()
    {
        using var directory = new OutputDirectory();
        File.WriteAllBytes(directory.Target, [1, 2, 3]);
        File.SetLastWriteTimeUtc(directory.Target, new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc));
        var timestamp = File.GetLastWriteTimeUtc(directory.Target);
        var publisher = new TypeGenerationPackagePublisher(
            _ => throw new InvalidOperationException("Staging must not be opened."),
            (_, _) => throw new InvalidOperationException("Commit must not run."),
            _ => throw new InvalidOperationException("Cleanup must not run."));
        var result = await publisher.PublishAsync(directory.Target, [1, 2, 3]);
        Assert.False(result.Changed);
        Assert.Equal(timestamp, File.GetLastWriteTimeUtc(directory.Target));
        Assert.Equal<byte>([1, 2, 3], File.ReadAllBytes(directory.Target));
    }

    [Fact]
    public async Task CommitUsesACompleteSameDirectoryFileAndDoesNotDeleteTheOldTargetFirst()
    {
        using var directory = new OutputDirectory();
        File.WriteAllBytes(directory.Target, [9, 8, 7]);
        var committed = false;
        var publisher = new TypeGenerationPackagePublisher(Create, (temporary, target) =>
        {
            Assert.Equal(Path.GetDirectoryName(target), Path.GetDirectoryName(temporary));
            Assert.Equal<byte>([9, 8, 7], File.ReadAllBytes(target));
            Assert.Equal<byte>([1, 2, 3], File.ReadAllBytes(temporary));
            File.Move(temporary, target, overwrite: true);
            committed = true;
        }, File.Delete);
        var result = await publisher.PublishAsync(directory.Target, [1, 2, 3]);
        Assert.True(committed);
        Assert.True(result.Changed);
        Assert.Equal<byte>([1, 2, 3], File.ReadAllBytes(directory.Target));
        Assert.Empty(Directory.GetFiles(directory.Path, "*.tmp"));
    }

    [Theory]
    [InlineData(false, "write")]
    [InlineData(true, "write")]
    [InlineData(false, "commit")]
    [InlineData(true, "commit")]
    [InlineData(false, "flush")]
    [InlineData(true, "flush")]
    public async Task FailuresBeforeCommit_PreserveExistingOutputAndRemoveOwnedTemporaryFiles(bool existing, string phase)
    {
        using var directory = new OutputDirectory();
        if (existing)
        {
            File.WriteAllBytes(directory.Target, [9, 8, 7]);
            File.SetLastWriteTimeUtc(directory.Target, new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc));
        }
        var timestamp = existing ? File.GetLastWriteTimeUtc(directory.Target) : default;
        var publisher = new TypeGenerationPackagePublisher(
            path => new FaultStream(path, phase),
            (_, _) => throw new IOException("Injected commit failure."),
            File.Delete);
        var error = await Assert.ThrowsAsync<ToolException>(() => publisher.PublishAsync(directory.Target, [1, 2, 3, 4]));
        Assert.Equal("TGH1012", error.Code);
        Assert.Equal(1, error.ExitCode);
        Assert.Equal(existing, File.Exists(directory.Target));
        if (existing)
        {
            Assert.Equal<byte>([9, 8, 7], File.ReadAllBytes(directory.Target));
            Assert.Equal(timestamp, File.GetLastWriteTimeUtc(directory.Target));
        }
        Assert.Empty(Directory.GetFiles(directory.Path, "*.tmp"));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task CancellationDuringWriteOrAfterFlush_NeverCommits(bool existing, bool afterFlush)
    {
        using var directory = new OutputDirectory();
        if (existing) { File.WriteAllBytes(directory.Target, [9, 8, 7]); }
        var timestamp = existing ? File.GetLastWriteTimeUtc(directory.Target) : default;
        using var cancellation = new CancellationTokenSource();
        var publisher = new TypeGenerationPackagePublisher(
            path => new CancellingStream(path, cancellation, afterFlush),
            (_, _) => throw new InvalidOperationException("Commit must not run."),
            File.Delete);
        var error = await Assert.ThrowsAsync<OperationCanceledException>(() =>
            publisher.PublishAsync(directory.Target, [1, 2, 3, 4], cancellation.Token));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(existing, File.Exists(directory.Target));
        if (existing)
        {
            Assert.Equal<byte>([9, 8, 7], File.ReadAllBytes(directory.Target));
            Assert.Equal(timestamp, File.GetLastWriteTimeUtc(directory.Target));
        }
        Assert.Empty(Directory.GetFiles(directory.Path, "*.tmp"));
    }

    [Fact]
    public async Task CancellationAfterCommit_DoesNotMisreportThePublishedFile()
    {
        using var directory = new OutputDirectory();
        using var cancellation = new CancellationTokenSource();
        var publisher = new TypeGenerationPackagePublisher(Create, (temporary, target) =>
        {
            File.Move(temporary, target, overwrite: true);
            cancellation.Cancel();
        }, File.Delete);
        var result = await publisher.PublishAsync(directory.Target, [1, 2, 3], cancellation.Token);
        Assert.True(result.Changed);
        Assert.Equal<byte>([1, 2, 3], File.ReadAllBytes(directory.Target));
    }

    [Fact]
    public async Task FailedCreation_DoesNotDeleteAnUnownedFile()
    {
        using var directory = new OutputDirectory();
        string? collided = null;
        var publisher = new TypeGenerationPackagePublisher(path =>
        {
            File.WriteAllBytes(path, [8, 8]);
            collided = path;
            throw new IOException("Simulated CreateNew collision.");
        }, (_, _) => throw new InvalidOperationException(), _ => throw new InvalidOperationException("Must not delete an unowned file."));
        var error = await Assert.ThrowsAsync<ToolException>(() => publisher.PublishAsync(directory.Target, [1, 2, 3]));
        Assert.Equal("TGH1012", error.Code);
        Assert.NotNull(collided);
        Assert.Equal<byte>([8, 8], File.ReadAllBytes(collided));
        Assert.False(File.Exists(directory.Target));
    }

    [Fact]
    public async Task CleanupFailure_IsExplicitAndRetainsTheOriginalFailureContext()
    {
        using var directory = new OutputDirectory();
        File.WriteAllBytes(directory.Target, [9]);
        var publisher = new TypeGenerationPackagePublisher(path => new FaultStream(path, "write"),
            (_, _) => throw new InvalidOperationException(), _ => throw new IOException("Injected cleanup failure."));
        var error = await Assert.ThrowsAsync<ToolException>(() => publisher.PublishAsync(directory.Target, [1, 2, 3]));
        Assert.Equal("TGH1013", error.Code);
        Assert.Contains("Injected write failure", error.Message);
        Assert.Contains("Injected cleanup failure", error.Message);
        Assert.Equal<byte>([9], File.ReadAllBytes(directory.Target));
        Assert.Single(Directory.GetFiles(directory.Path, "*.tmp"));
    }

    [Fact]
    public async Task LockedTargetAndParentFile_AreExplicitIoFailures()
    {
        using var directory = new OutputDirectory();
        File.WriteAllBytes(directory.Target, [1, 2, 3]);
        using (var locked = new FileStream(directory.Target, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            var error = await Assert.ThrowsAsync<ToolException>(() => new TypeGenerationPackagePublisher().PublishAsync(directory.Target, [4, 5, 6]));
            Assert.Equal("TGH1012", error.Code);
        }
        var parentFile = Path.Combine(directory.Path, "parent-file");
        File.WriteAllText(parentFile, "preserve");
        var failure = await Assert.ThrowsAsync<ToolException>(() =>
            new TypeGenerationPackagePublisher().PublishAsync(Path.Combine(parentFile, "types.tgz"), [1]));
        Assert.Equal("TGH1012", failure.Code);
        Assert.Equal("preserve", File.ReadAllText(parentFile));
        Assert.Empty(Directory.GetFiles(directory.Path, "*.tmp"));
    }

    [Fact]
    public async Task PreCancellationAndInvalidPayloads_DoNotCreateDirectories()
    {
        using var directory = new OutputDirectory();
        var path = Path.Combine(directory.Path, "new", "types.tgz");
        var publisher = new TypeGenerationPackagePublisher();
        Assert.Equal("archiveBytes", (await Assert.ThrowsAsync<ArgumentException>(() => publisher.PublishAsync(path, default))).ParamName);
        await Assert.ThrowsAsync<ArgumentException>(() => publisher.PublishAsync(path, []));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => publisher.PublishAsync(path, [1], cancellation.Token));
        Assert.False(Directory.Exists(Path.GetDirectoryName(path)));
    }

    [Fact]
    public void PathValidation_HandlesRelativePathsAndRejectsDirectoryAndInputCollisions()
    {
        using var directory = new OutputDirectory();
        var relative = Path.GetRelativePath(Environment.CurrentDirectory, Path.Combine(directory.Path, "space dir", "types.tgz"));
        Assert.Equal(Path.GetFullPath(relative), TypeGenerationOutputPath.Resolve(relative));
        Assert.False(Directory.Exists(Path.GetDirectoryName(Path.GetFullPath(relative))));
        Assert.Equal("TGH1011", Assert.Throws<ToolException>(() => TypeGenerationOutputPath.Resolve(directory.Path)).Code);
        var project = Path.Combine(directory.Path, "Input.csproj");
        File.WriteAllText(project, "<Project />");
        var same = TypeGenerationOutputPath.Resolve(Path.Combine(directory.Path, "unused", "..", "Input.csproj"));
        Assert.Equal("TGH1011", Assert.Throws<ToolException>(() => TypeGenerationOutputPath.RejectInputCollision(same, project)).Code);
        var source = Path.Combine(directory.Path, "Model.cs");
        File.WriteAllText(source, "preserve");
        Assert.Equal("TGH1011", Assert.Throws<ToolException>(() => TypeGenerationOutputPath.RejectInputCollision(
            TypeGenerationOutputPath.Resolve(source), project, [source])).Code);
        Assert.Equal("preserve", File.ReadAllText(source));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("bad\0path")]
    public void InvalidPaths_AreArgumentDiagnostics(string path)
    {
        var error = Assert.Throws<ToolException>(() => TypeGenerationOutputPath.Resolve(path));
        Assert.Equal("TGH1011", error.Code);
        Assert.Equal(2, error.ExitCode);
    }

    [Theory]
    [InlineData("types.tgz.")]
    [InlineData("types.tgz ")]
    public void WindowsAliases_AreRejectedBeforeFullPathNormalization(string name)
    {
        if (!OperatingSystem.IsWindows()) { return; }
        using var directory = new OutputDirectory();
        var error = Assert.Throws<ToolException>(() => TypeGenerationOutputPath.Resolve(Path.Combine(directory.Path, name)));
        Assert.Equal("TGH1011", error.Code);
        Assert.Empty(Directory.GetFiles(directory.Path));
    }

    private static FileStream Create(string path) =>
        new(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous);

    private sealed class FaultStream(string path, string phase)
        : FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous)
    {
        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await base.WriteAsync(buffer[..1], cancellationToken);
            if (phase == "write") { throw new IOException("Injected write failure."); }
            await base.WriteAsync(buffer[1..], cancellationToken);
        }

        public override void Flush(bool flushToDisk)
        {
            base.Flush(flushToDisk);
            if (phase == "flush" && flushToDisk) { throw new IOException("Injected flush failure."); }
        }
    }

    private sealed class CancellingStream(string path, CancellationTokenSource cancellation, bool afterFlush)
        : FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous)
    {
        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (afterFlush) { await base.WriteAsync(buffer, cancellationToken); return; }
            await base.WriteAsync(buffer[..1], cancellationToken);
            cancellation.Cancel();
            cancellationToken.ThrowIfCancellationRequested();
        }

        public override void Flush(bool flushToDisk)
        {
            base.Flush(flushToDisk);
            if (afterFlush && flushToDisk) { cancellation.Cancel(); }
        }
    }

    private sealed class OutputDirectory : IDisposable
    {
        public string Path { get; } = Directory.CreateTempSubdirectory("TypeGenerationPublishTests-").FullName;

        public string Target => System.IO.Path.Combine(Path, "types.tgz");

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
