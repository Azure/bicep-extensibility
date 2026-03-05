// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Json.Pointer;

namespace Azure.Deployments.Extensibility.Core.V2.Models
{
    public record ResourceMetadata
    {
        public ISet<JsonPointer>? Calculated { get; set; }

        public ISet<JsonPointer>? Immutable { get; set; }

        public ISet<JsonPointer>? ReadOnly { get; set; }

        public ISet<JsonPointer>? Unevaluated { get; set; }

        public ISet<JsonPointer>? Unknown { get; set; }
    }
}
