// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Azure.Deployments.Extensibility.Core.Json;

namespace Azure.Deployments.Extensibility.Core.V2.Models
{
    public record ResourceMetadata
    {
        [JsonConverter(typeof(OrdinalIgnoreCaseStringSetConverter))]
        public ISet<string>? Calculated { get; set; }

        [JsonConverter(typeof(OrdinalIgnoreCaseStringSetConverter))]
        public ISet<string>? Immutable { get; set; }

        [JsonConverter(typeof(OrdinalIgnoreCaseStringSetConverter))]
        public ISet<string>? ReadOnly { get; set; }

        [JsonConverter(typeof(OrdinalIgnoreCaseStringSetConverter))]
        public ISet<string>? Unevaluated { get; set; }

        [JsonConverter(typeof(OrdinalIgnoreCaseStringSetConverter))]
        public ISet<string>? Unknown { get; set; }
    }
}
