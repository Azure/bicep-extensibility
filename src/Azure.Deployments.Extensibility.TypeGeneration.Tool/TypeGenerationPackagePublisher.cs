// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;

namespace Azure.Deployments.Extensibility.TypeGeneration.Tool;

internal sealed record PackagePublishResult(string OutputPath, bool Changed);

internal sealed class TypeGenerationPackagePublisher
{
    private readonly Func<string, FileStream> createStagingFile;
    private readonly Action<string, string> commitFile;
    private readonly Action<string> deleteStagingFile;

    public TypeGenerationPackagePublisher()
        : this(
            path => new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous),
            (source, destination) => File.Move(source, destination, overwrite: true),
            File.Delete)
    {
    }

    internal TypeGenerationPackagePublisher(
        Func<string, FileStream> createStagingFile,
        Action<string, string> commitFile,
        Action<string> deleteStagingFile)
    {
        ArgumentNullException.ThrowIfNull(createStagingFile);
        ArgumentNullException.ThrowIfNull(commitFile);
        ArgumentNullException.ThrowIfNull(deleteStagingFile);
        this.createStagingFile = createStagingFile;
        this.commitFile = commitFile;
        this.deleteStagingFile = deleteStagingFile;
    }

    public async Task<PackagePublishResult> PublishAsync(
        string outputPath,
        ImmutableArray<byte> archiveBytes,
        CancellationToken cancellationToken = default)
    {
        if (archiveBytes.IsDefaultOrEmpty)
        {
            throw new ArgumentException("Archive bytes must be initialized and nonempty.", nameof(archiveBytes));
        }
        cancellationToken.ThrowIfCancellationRequested();
        var destination = TypeGenerationOutputPath.Resolve(outputPath);
        string? ownedTemporaryPath = null;
        Exception? primaryFailure = null;
        try
        {
            if (await HasSameContentAsync(destination, archiveBytes, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                return new(destination, false);
            }

            cancellationToken.ThrowIfCancellationRequested();
            var directory = Path.GetDirectoryName(destination)!;
            Directory.CreateDirectory(directory);
            var temporary = Path.Combine(directory, $".bicep-typegen-{Guid.NewGuid():N}.tmp");
            await using (var stream = createStagingFile(temporary))
            {
                ownedTemporaryPath = temporary;
                await stream.WriteAsync(archiveBytes.AsMemory(), cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                stream.Flush(flushToDisk: true);
            }

            TypeGenerationOutputPath.ValidateLeaf(destination);
            cancellationToken.ThrowIfCancellationRequested();
            commitFile(temporary, destination);
            ownedTemporaryPath = null;
            return new(destination, true);
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            primaryFailure = exception;
            throw;
        }
        catch (ToolException exception)
        {
            primaryFailure = exception;
            throw;
        }
        catch (IOException exception)
        {
            primaryFailure = exception;
            throw new ToolException("TGH1012", $"Cannot publish package to '{destination}': {exception.Message}", exception);
        }
        catch (UnauthorizedAccessException exception)
        {
            primaryFailure = exception;
            throw new ToolException("TGH1012", $"Cannot publish package to '{destination}': {exception.Message}", exception);
        }
        finally
        {
            if (ownedTemporaryPath is { } temporary)
            {
                try
                {
                    deleteStagingFile(temporary);
                }
                catch (IOException exception)
                {
                    throw CleanupFailure(temporary, destination, primaryFailure, exception);
                }
                catch (UnauthorizedAccessException exception)
                {
                    throw CleanupFailure(temporary, destination, primaryFailure, exception);
                }
            }
        }
    }

    private static async Task<bool> HasSameContentAsync(
        string path,
        ImmutableArray<byte> content,
        CancellationToken cancellationToken)
    {
        FileStream stream;
        try
        {
            stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
        }
        catch (FileNotFoundException) { return false; }
        catch (DirectoryNotFoundException) { return false; }

        await using (stream)
        {
            if (stream.Length != content.Length) { return false; }
            var buffer = new byte[Math.Min(81920, content.Length)];
            var offset = 0;
            while (offset < content.Length)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(0, Math.Min(buffer.Length, content.Length - offset)), cancellationToken);
                if (read == 0 || !content.AsSpan(offset, read).SequenceEqual(buffer.AsSpan(0, read))) { return false; }
                offset += read;
            }
            return true;
        }
    }

    private static ToolException CleanupFailure(string temporary, string destination, Exception? primary, Exception cleanup) =>
        new("TGH1013",
            $"Package was not committed to '{destination}'. Cannot remove staging file '{temporary}': {cleanup.Message}. " +
            $"Original operation: {primary?.Message ?? "not committed"}.",
            cleanup);
}
