// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Formats.Tar;
using System.IO.Compression;
using Azure.Deployments.Extensibility.TypeGeneration.Backend.Serialization;
using Azure.Deployments.Extensibility.TypeGeneration.Backend.Validation;
using Azure.Deployments.Extensibility.TypeGeneration.Diagnostics;

namespace Azure.Deployments.Extensibility.TypeGeneration.Backend.Packaging;

internal static class TypeGenerationArchiveValidator
{
    public static TypeGenerationValidationResult Validate(
        ImmutableArray<byte> archive,
        TypeGenerationValidatedJsonPackage expected,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(expected);
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            Require(!archive.IsDefaultOrEmpty, "Archive bytes are missing.");
            var limit = TypeGenerationArchiveWriter.ExpectedTarLength(expected.Files);
            Require(limit <= int.MaxValue, "Archive exceeds the supported in-memory size.");
            using var input = new MemoryStream(archive.ToArray(), writable: false);
            byte[] tar;
            using (var gzip = new GZipStream(input, CompressionMode.Decompress, leaveOpen: true))
            {
                tar = ReadBounded(gzip, limit, cancellationToken);
            }
            Require(tar.LongLength == limit, "Expanded archive length does not match the fixed package layout.");

            // GZipStream can accept trailing input; our own output profile must contain exactly one canonical member.
            Require(archive.AsSpan().SequenceEqual(TypeGenerationArchiveWriter.Compress(tar, cancellationToken).AsSpan()),
                "Compressed framing is truncated, has trailing data, or is not the current writer's canonical encoding.");

            using var stream = new MemoryStream(tar, writable: false);
            using var reader = new TarReader(stream);
            var offset = 0;
            var index = ReadEntry(TypeGenerationSerializedPackage.IndexFileName, expected.Files.IndexJson);
            var types = ReadEntry(TypeGenerationSerializedPackage.TypesFileName, expected.Files.TypesJson);
            Require(reader.GetNextEntry() is null, "Archive contains an unexpected extra entry.");
            Require(tar.Length - offset == 2 * TypeGenerationArchiveWriter.BlockSize, "Archive end markers are missing.");
            RequireZeros(tar.AsSpan(offset), "Archive end markers must contain only zero bytes.");
            cancellationToken.ThrowIfCancellationRequested();
            return TypeGenerationPackageValidator.Validate(new(index, types), cancellationToken: cancellationToken);

            ImmutableArray<byte> ReadEntry(string name, ImmutableArray<byte> expectedBytes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var entry = reader.GetNextEntry();
                Require(entry is UstarTarEntry, $"Entry '{name}' is missing or is not Ustar.");
                var file = (UstarTarEntry)entry!;
                Require(file.EntryType == TarEntryType.RegularFile, $"Entry '{name}' must be a regular file.");
                Require(file.Name == name, $"Expected root entry '{name}', found '{file.Name}'.");
                Require(file.Uid == 0 && file.Gid == 0 && file.UserName.Length == 0 && file.GroupName.Length == 0,
                    $"Entry '{name}' has noncanonical owner/group metadata.");
                Require(file.Mode == TypeGenerationArchiveWriter.FileMode && file.ModificationTime == DateTimeOffset.UnixEpoch,
                    $"Entry '{name}' has noncanonical mode/timestamp metadata.");
                Require(file.LinkName.Length == 0, $"Entry '{name}' must not carry a link target.");
                Require(file.Length == expectedBytes.Length && file.DataStream is not null,
                    $"Entry '{name}' has missing or unexpected content length.");
                var bytes = ReadBounded(file.DataStream!, expectedBytes.Length, cancellationToken);
                Require(bytes.AsSpan().SequenceEqual(expectedBytes.AsSpan()), $"Entry '{name}' differs from the validated JSON.");
                var padding = checked((int)TypeGenerationArchiveWriter.PaddedLength(bytes.Length) - bytes.Length);
                RequireZeros(tar.AsSpan(offset + TypeGenerationArchiveWriter.BlockSize + bytes.Length, padding),
                    $"Entry '{name}' has nonzero data padding.");
                offset = checked(offset + TypeGenerationArchiveWriter.BlockSize + bytes.Length + padding);
                return bytes.ToImmutableArray();
            }
        }
        catch (InvalidDataException exception)
        {
            return Failure(exception);
        }
        catch (IOException exception)
        {
            return Failure(exception);
        }
        catch (ArgumentException exception)
        {
            return Failure(exception);
        }
        catch (OverflowException exception)
        {
            return Failure(exception);
        }
    }

    private static byte[] ReadBounded(Stream source, long limit, CancellationToken cancellationToken)
    {
        using var output = new MemoryStream();
        var buffer = new byte[8192];
        int read;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            read = source.Read(buffer, 0, buffer.Length);
            if (read == 0) { break; }
            Require(output.Length + read <= limit, "Archive expands beyond the expected content size.");
            output.Write(buffer, 0, read);
        }
        return output.ToArray();
    }

    private static void RequireZeros(ReadOnlySpan<byte> bytes, string message)
    {
        foreach (var value in bytes)
        {
            if (value != 0) { throw new InvalidDataException(message); }
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) { throw new InvalidDataException(message); }
    }

    private static TypeGenerationValidationResult Failure(Exception exception) =>
        new(null, [new(TypeGenDiagnosticCodes.BackendInvalidArchive, TypeGenDiagnosticSeverity.Error,
            $"Invalid 'types.tgz': {exception.Message}", null)]);
}
