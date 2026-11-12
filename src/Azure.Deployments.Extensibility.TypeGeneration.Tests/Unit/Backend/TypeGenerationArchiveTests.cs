// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Formats.Tar;
using System.IO.Compression;
using System.Text;
using Azure.Deployments.Extensibility.TypeGeneration.Backend;
using Azure.Deployments.Extensibility.TypeGeneration.Backend.Packaging;
using Azure.Deployments.Extensibility.TypeGeneration.Backend.Serialization;
using Azure.Deployments.Extensibility.TypeGeneration.Backend.Validation;
using Azure.Deployments.Extensibility.TypeGeneration.Diagnostics;
using Azure.Deployments.Extensibility.TypeGeneration.Semantics;
using Azure.Deployments.Extensibility.TypeGeneration.Tests.Support;
using Xunit;

namespace Azure.Deployments.Extensibility.TypeGeneration.Tests.Unit.Backend;

[Trait("Category", "Unit")]
public class TypeGenerationArchiveTests
{
    [Fact]
    public void Archive_HasExactlyTwoFixedRegularFilesAndIndependentContentValidation()
    {
        var expected = Validated(ContosoSampleFixture.CreateExpected());
        var package = Write(expected);
        Assert.Equal<byte>(expected.Files.IndexJson, package.IndexJson);
        Assert.Equal<byte>(expected.Files.TypesJson, package.TypesJson);
        Assert.Equal<byte>([0x1f, 0x8b, 8, 0, 0, 0, 0, 0], package.ArchiveBytes.Take(8));
        using var compressed = new MemoryStream(package.ArchiveBytes.ToArray(), writable: false);
        using var gzip = new GZipStream(compressed, CompressionMode.Decompress);
        using var reader = new TarReader(gzip);
        var index = Read("index.json");
        var types = Read("types.json");
        Assert.Null(reader.GetNextEntry());
        Assert.Equal<byte>(package.IndexJson, index);
        Assert.Equal<byte>(package.TypesJson, types);
        Assert.True(TypeGenerationPackageValidator.Validate(new(index, types)).Succeeded);

        ImmutableArray<byte> Read(string name)
        {
            var entry = Assert.IsType<UstarTarEntry>(reader.GetNextEntry());
            Assert.Equal(name, entry.Name);
            Assert.Equal(TarEntryType.RegularFile, entry.EntryType);
            Assert.Equal(TarEntryFormat.Ustar, entry.Format);
            Assert.Equal(DateTimeOffset.UnixEpoch, entry.ModificationTime);
            Assert.Equal(0, entry.Uid);
            Assert.Equal(0, entry.Gid);
            Assert.Equal("", entry.UserName);
            Assert.Equal("", entry.GroupName);
            Assert.Equal("", entry.LinkName);
            Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.OtherRead, entry.Mode);
            using var data = new MemoryStream();
            Assert.NotNull(entry.DataStream);
            entry.DataStream.CopyTo(data);
            return data.ToArray().ToImmutableArray();
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void EmptyAndConfigurationOnlyInputs_StillHaveBothFiles(bool configuration, bool singleton)
    {
        var model = new ExtensionTypeSemanticModel
        {
            Name = "Empty", Version = "1", IsSingleton = singleton, ResourceTypes = [],
            Configuration = configuration ? new ObjectTypeDefinition { Name = "Config", Properties = [] } : null,
        };
        var expected = Validated(model);
        var result = Write(expected);
        var validation = TypeGenerationArchiveValidator.Validate(result.ArchiveBytes, expected);
        Assert.True(validation.Succeeded);
        Assert.Equal<byte>(expected.Files.IndexJson, validation.Model!.Files.IndexJson);
        Assert.Equal<byte>(expected.Files.TypesJson, validation.Model.Files.TypesJson);
    }

    [Fact]
    public async Task RepeatedAndConcurrentArchives_AreByteIdentical()
    {
        var expected = Validated(ContosoSampleFixture.CreateExpected());
        var first = Write(expected);
        var results = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => Task.Run(() => Write(expected))));
        Assert.All(results, package => Assert.Equal<byte>(first.ArchiveBytes, package.ArchiveBytes));
        Assert.Equal<byte>(expected.Files.IndexJson, first.IndexJson);
        Assert.Equal<byte>(expected.Files.TypesJson, first.TypesJson);
    }

    [Theory]
    [InlineData("default")]
    [InlineData("empty")]
    [InlineData("not-gzip")]
    [InlineData("truncated")]
    [InlineData("missing-footer")]
    [InlineData("crc")]
    [InlineData("trailing")]
    [InlineData("duplicate-footer")]
    [InlineData("concatenated")]
    public void CorruptedCompressedFraming_IsRejected(string scenario)
    {
        var expected = Validated(SemanticModelFixtures.CreateContoso());
        var original = Write(expected).ArchiveBytes;
        var mutated = scenario switch
        {
            "default" => default,
            "empty" => ImmutableArray<byte>.Empty,
            "not-gzip" => ImmutableArray.Create<byte>(1, 2, 3),
            "truncated" => original.RemoveAt(original.Length - 1),
            "missing-footer" => original.RemoveRange(original.Length - 8, 8),
            "crc" => original.SetItem(original.Length - 8, (byte)(original[^8] ^ 0xff)),
            "trailing" => original.AddRange(new byte[] { 1, 2, 3 }),
            "duplicate-footer" => original.AddRange(original.AsSpan(original.Length - 8, 8).ToArray()),
            _ => original.AddRange(original),
        };
        Invalid(mutated, expected);
    }

    [Theory]
    [InlineData("missing-entry")]
    [InlineData("extra-entry")]
    [InlineData("duplicate-entry")]
    [InlineData("reversed-order")]
    [InlineData("rooted-path")]
    [InlineData("parent-path")]
    [InlineData("prefixed-path")]
    [InlineData("backslash")]
    [InlineData("timestamp")]
    [InlineData("uid")]
    [InlineData("gid")]
    [InlineData("owner")]
    [InlineData("group")]
    [InlineData("mode")]
    [InlineData("directory")]
    [InlineData("symlink")]
    [InlineData("hardlink")]
    public void InvalidTarLayoutOrMetadata_IsRejected(string scenario)
    {
        var expected = Validated(SemanticModelFixtures.CreateContoso());
        var files = new List<(string Name, ImmutableArray<byte> Bytes)>
        {
            ("index.json", expected.Files.IndexJson), ("types.json", expected.Files.TypesJson),
        };
        switch (scenario)
        {
            case "missing-entry": files.RemoveAt(1); break;
            case "extra-entry": files.Add(("extra.json", [1])); break;
            case "duplicate-entry": files[1] = ("index.json", expected.Files.TypesJson); break;
            case "reversed-order": files.Reverse(); break;
            case "rooted-path": files[0] = ("/index.json", expected.Files.IndexJson); break;
            case "parent-path": files[0] = ("../index.json", expected.Files.IndexJson); break;
            case "prefixed-path": files[0] = ("package/index.json", expected.Files.IndexJson); break;
            case "backslash": files[0] = ("package\\index.json", expected.Files.IndexJson); break;
        }
        var archive = CreateArchive(files, (entry, position) =>
        {
            if (position != 0) { return; }
            switch (scenario)
            {
                case "timestamp": entry.ModificationTime = DateTimeOffset.UnixEpoch.AddSeconds(1); break;
                case "uid": entry.Uid = 1; break;
                case "gid": entry.Gid = 1; break;
                case "owner": entry.UserName = "owner"; break;
                case "group": entry.GroupName = "group"; break;
                case "mode": entry.Mode |= UnixFileMode.UserExecute; break;
            }
        }, scenario switch
        {
            "directory" => TarEntryType.Directory,
            "symlink" => TarEntryType.SymbolicLink,
            "hardlink" => TarEntryType.HardLink,
            _ => TarEntryType.RegularFile,
        });
        Invalid(archive, expected);
    }

    [Theory]
    [InlineData("header-checksum")]
    [InlineData("end-markers")]
    [InlineData("missing-end-block")]
    [InlineData("extra-zero-block")]
    [InlineData("padding")]
    public void MalformedUncompressedTar_IsRejected(string scenario)
    {
        var expected = Validated(SemanticModelFixtures.CreateContoso());
        var original = Write(expected).ArchiveBytes;
        using var compressed = new MemoryStream(original.ToArray());
        using var gzip = new GZipStream(compressed, CompressionMode.Decompress);
        using var uncompressed = new MemoryStream();
        gzip.CopyTo(uncompressed);
        var tar = uncompressed.ToArray();
        switch (scenario)
        {
            case "header-checksum": tar[0] ^= 1; break;
            case "end-markers": tar[^1] = 1; break;
            case "missing-end-block": tar = tar[..^512]; break;
            case "extra-zero-block": tar = [.. tar, .. new byte[512]]; break;
            case "padding":
                Assert.NotEqual(0, expected.Files.IndexJson.Length % 512);
                tar[512 + expected.Files.IndexJson.Length] = 1;
                break;
        }
        Invalid(Compress(tar), expected);
    }

    [Fact]
    public void ArchiveContent_MustMatchThePreviouslyValidatedFilesExactly()
    {
        var expected = Validated(SemanticModelFixtures.CreateContoso());
        var changed = expected.Files.IndexJson.ToArray();
        var position = Array.IndexOf(changed, (byte)'C');
        Assert.True(position >= 0);
        changed[position] = (byte)'D';
        var archive = CreateArchive([("index.json", changed.ToImmutableArray()), ("types.json", expected.Files.TypesJson)]);
        Invalid(archive, expected);
    }

    [Fact]
    public void ForgedValidationMarker_DoesNotBypassTheReadBackJsonCheck()
    {
        var invalid = new TypeGenerationValidatedJsonPackage(new(Encoding.UTF8.GetBytes("{}").ToImmutableArray(), Encoding.UTF8.GetBytes("[]").ToImmutableArray()));
        var result = TypeGenerationArchiveWriter.Write(invalid);
        Assert.False(result.Succeeded);
        Assert.Null(result.Package);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "TG2402");
    }

    [Fact]
    public void Validator_IsExplicitlyForCurrentWriterEncoding_NotAnArbitraryArchiveImporter()
    {
        var expected = Validated(SemanticModelFixtures.CreateContoso());
        var archive = CreateArchive([("index.json", expected.Files.IndexJson), ("types.json", expected.Files.TypesJson)], level: CompressionLevel.NoCompression);
        Invalid(archive, expected);
    }

    [Fact]
    public void CancellationAndNullArguments_DoNotProduceFailureShapedPackages()
    {
        Assert.Throws<ArgumentNullException>(() => TypeGenerationArchiveWriter.Write(null!));
        Assert.Throws<ArgumentNullException>(() => TypeGenerationArchiveValidator.Validate([], null!));
        var expected = Validated(SemanticModelFixtures.CreateContoso());
        var bytes = Write(expected).ArchiveBytes;
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Equal(cancellation.Token, Assert.Throws<OperationCanceledException>(() => TypeGenerationArchiveWriter.Write(expected, cancellation.Token)).CancellationToken);
        Assert.Equal(cancellation.Token, Assert.Throws<OperationCanceledException>(() => TypeGenerationArchiveValidator.Validate(bytes, expected, cancellation.Token)).CancellationToken);
        Assert.Equal("TG2501", TypeGenDiagnosticCodes.BackendArchiveWriteFailure);
        Assert.Equal("TG2502", TypeGenDiagnosticCodes.BackendInvalidArchive);
    }

    private static TypeGenerationValidatedJsonPackage Validated(ExtensionTypeSemanticModel source)
    {
        var result = TypeGenerationJsonGenerator.Generate(source);
        Assert.True(result.Succeeded);
        return Assert.IsType<TypeGenerationValidatedJsonPackage>(result.Model);
    }

    private static TypeGenerationPackage Write(TypeGenerationValidatedJsonPackage expected)
    {
        var result = TypeGenerationArchiveWriter.Write(expected);
        Assert.True(result.Succeeded, string.Join(Environment.NewLine, result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        Assert.Empty(result.Diagnostics);
        return Assert.IsType<TypeGenerationPackage>(result.Package);
    }

    private static void Invalid(ImmutableArray<byte> archive, TypeGenerationValidatedJsonPackage expected)
    {
        var result = TypeGenerationArchiveValidator.Validate(archive, expected);
        Assert.False(result.Succeeded);
        Assert.Null(result.Model);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("TG2502", diagnostic.Code);
        Assert.Contains("types.tgz", diagnostic.Message, StringComparison.Ordinal);
    }

    private static ImmutableArray<byte> CreateArchive(
        IEnumerable<(string Name, ImmutableArray<byte> Bytes)> files,
        Action<UstarTarEntry, int>? change = null,
        TarEntryType firstType = TarEntryType.RegularFile,
        CompressionLevel level = CompressionLevel.Optimal)
    {
        using var tar = new MemoryStream();
        using (var writer = new TarWriter(tar, TarEntryFormat.Ustar, leaveOpen: true))
        {
            var position = 0;
            foreach (var (name, bytes) in files)
            {
                var kind = position == 0 ? firstType : TarEntryType.RegularFile;
                var entry = new UstarTarEntry(kind, name)
                {
                    ModificationTime = DateTimeOffset.UnixEpoch, Uid = 0, Gid = 0,
                    UserName = "", GroupName = "",
                    Mode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.OtherRead,
                };
                using var content = new MemoryStream(bytes.ToArray());
                if (kind == TarEntryType.RegularFile) { entry.DataStream = content; }
                if (kind is TarEntryType.SymbolicLink or TarEntryType.HardLink) { entry.LinkName = "target"; }
                change?.Invoke(entry, position++);
                writer.WriteEntry(entry);
            }
        }
        return Compress(tar.ToArray(), level);
    }

    private static ImmutableArray<byte> Compress(byte[] tar, CompressionLevel level = CompressionLevel.Optimal)
    {
        using var result = new MemoryStream();
        using (var gzip = new GZipStream(result, level, leaveOpen: true)) { gzip.Write(tar); }
        return result.ToArray().ToImmutableArray();
    }
}
