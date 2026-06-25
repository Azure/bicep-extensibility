// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using FluentAssertions;
using Json.Pointer;
using Xunit;

namespace Azure.Deployments.Extensibility.Core.Tests.Unit.V2.Contracts.Models
{
    public class ResourcePreviewMetadataTests
    {
        [Fact]
        public void Build_WithNoProperties_ReturnsAllNull()
        {
            var metadata = ResourcePreviewMetadata.NewBuilder().Build();

            metadata.ReadOnly.Should().BeNull();
            metadata.Immutable.Should().BeNull();
            metadata.Unknown.Should().BeNull();
            metadata.Calculated.Should().BeNull();
            metadata.Unevaluated.Should().BeNull();
        }

        [Fact]
        public void Build_WithAllProperties_ReturnsExpectedObject()
        {
            var readOnly1 = JsonPointer.Parse("/a");
            var readOnly2 = JsonPointer.Parse("/b");
            var readOnly3 = JsonPointer.Parse("/c");
            var readOnly4 = JsonPointer.Parse("/d");
            var readOnlyDupe = JsonPointer.Parse("/c");

            var immutable1 = JsonPointer.Parse("/d");
            var immutable2 = JsonPointer.Parse("/e");
            var immutable3 = JsonPointer.Parse("/f");
            var immutable4 = JsonPointer.Parse("/g");
            var immutableDupe = JsonPointer.Parse("/d");

            var unknown1 = JsonPointer.Parse("/a/b");
            var unknown2 = JsonPointer.Parse("/a/c");
            var unknown3 = JsonPointer.Parse("/b/c");
            var unknown4 = JsonPointer.Parse("/b/d");
            var unknownDupe = JsonPointer.Parse("/a/c");

            var calculated1 = JsonPointer.Parse("/h");
            var calculated2 = JsonPointer.Parse("/i");
            var calculated3 = JsonPointer.Parse("/j");
            var calculated4 = JsonPointer.Parse("/k");
            var calculatedDupe = JsonPointer.Parse("/h");

            var unevaluated1 = JsonPointer.Parse("/l");
            var unevaluated2 = JsonPointer.Parse("/m");
            var unevaluated3 = JsonPointer.Parse("/n");
            var unevaluated4 = JsonPointer.Parse("/o");
            var unevaluatedDupe = JsonPointer.Parse("/n");

            IEnumerable<JsonPointer>? nullEnumerable = null;

            var metadata = ResourcePreviewMetadata.NewBuilder()
                .WithReadOnly(readOnly1)
                .WithReadOnly(readOnly1, readOnly2)
                .WithReadOnly(new List<JsonPointer> { readOnly3, readOnly4, readOnlyDupe })
                .WithReadOnly(nullEnumerable)
                .WithImmutable(immutable1)
                .WithImmutable(immutable2, immutable3)
                .WithImmutable(new HashSet<JsonPointer> { immutable4, immutableDupe })
                .WithImmutable(nullEnumerable)
                .WithUnknown(unknown1, unknown2)
                .WithUnknown(unknown3)
                .WithUnknown(new List<JsonPointer> { unknown4, unknownDupe })
                .WithUnknown(nullEnumerable)
                .WithCalculated(calculated1, calculated2)
                .WithCalculated(calculatedDupe)
                .WithCalculated(new[] { calculated3, calculated4 })
                .WithCalculated(nullEnumerable)
                .WithUnevaluated(unevaluated1)
                .WithUnevaluated(unevaluated2, unevaluated3)
                .WithUnevaluated(new List<JsonPointer> { unevaluated4, unevaluatedDupe })
                .WithUnevaluated(nullEnumerable)
                .Build();

            metadata.ReadOnly.Should().NotBeNull();
            metadata.ReadOnly!.Value.Should().BeEquivalentTo([readOnly1, readOnly2, readOnly3, readOnly4]);

            metadata.Immutable.Should().NotBeNull();
            metadata.Immutable!.Value.Should().BeEquivalentTo([immutable1, immutable2, immutable3, immutable4]);

            metadata.Unknown.Should().NotBeNull();
            metadata.Unknown!.Value.Should().BeEquivalentTo([unknown1, unknown2, unknown3, unknown4]);

            metadata.Calculated.Should().NotBeNull();
            metadata.Calculated!.Value.Should().BeEquivalentTo([calculated1, calculated2, calculated3, calculated4]);

            metadata.Unevaluated.Should().NotBeNull();
            metadata.Unevaluated!.Value.Should().BeEquivalentTo([unevaluated1, unevaluated2, unevaluated3, unevaluated4]);
        }
    }
}
