// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Formats.Tar;
using System.IO.Compression;
using Azure.Deployments.Extensibility.TypeGeneration.Backend.Serialization;
using Azure.Deployments.Extensibility.TypeGeneration.Backend.Validation;
using Azure.Deployments.Extensibility.TypeGeneration.Diagnostics;

namespace Azure.Deployments.Extensibility.TypeGeneration.Backend.Packaging;

internal static class TypeGenerationArchiveWriter
{
    internal const int BlockSize = 512;
    internal const UnixFileMode FileMode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.OtherRead;

    public static TypeGenerationPackageResult Write(
        TypeGenerationValidatedJsonPackage package,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(package);
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var expectedLength = ExpectedTarLength(package.Files);
            if (expectedLength > int.MaxValue)
            {
                throw new InvalidDataException("The type archive exceeds the supported in-memory size.");
            }

            using var tar = new MemoryStream();
            using (var writer = new TarWriter(tar, TarEntryFormat.Ustar, leaveOpen: true))
            {
                WriteEntry(writer, TypeGenerationSerializedPackage.IndexFileName, package.Files.IndexJson, cancellationToken);
                WriteEntry(writer, TypeGenerationSerializedPackage.TypesFileName, package.Files.TypesJson, cancellationToken);
            }
            cancellationToken.ThrowIfCancellationRequested();
            var archive = Compress(tar.ToArray(), cancellationToken);
            var verified = TypeGenerationArchiveValidator.Validate(archive, package, cancellationToken);
            if (!verified.Succeeded)
            {
                return new(null, verified.Diagnostics);
            }

            cancellationToken.ThrowIfCancellationRequested();
            return new(new TypeGenerationPackage(verified.Model!.Files.IndexJson, verified.Model.Files.TypesJson, archive), verified.Diagnostics);
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
    }

    internal static long PaddedLength(int length) => ((long)length + BlockSize - 1) / BlockSize * BlockSize;

    internal static long ExpectedTarLength(TypeGenerationSerializedPackage files) =>
        4L * BlockSize + PaddedLength(files.IndexJson.Length) + PaddedLength(files.TypesJson.Length);

    internal static ImmutableArray<byte> Compress(ReadOnlySpan<byte> tar, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            gzip.Write(tar);
        }
        cancellationToken.ThrowIfCancellationRequested();
        return output.ToArray().ToImmutableArray();
    }

    private static void WriteEntry(TarWriter writer, string name, ImmutableArray<byte> content, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var data = new MemoryStream(content.ToArray(), writable: false);
        var entry = new UstarTarEntry(TarEntryType.RegularFile, name)
        {
            ModificationTime = DateTimeOffset.UnixEpoch,
            Uid = 0,
            Gid = 0,
            UserName = "",
            GroupName = "",
            Mode = FileMode,
            DataStream = data,
        };
        writer.WriteEntry(entry);
    }

    private static TypeGenerationPackageResult Failure(Exception exception) =>
        new(null, [new(TypeGenDiagnosticCodes.BackendArchiveWriteFailure, TypeGenDiagnosticSeverity.Error,
            $"Cannot create 'types.tgz': {exception.Message}", null)]);
}
