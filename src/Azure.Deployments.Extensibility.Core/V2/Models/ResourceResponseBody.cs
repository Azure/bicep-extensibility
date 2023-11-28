// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Core.V2.Models
{
    public record ResourceResponseBody
    {
        public ResourceResponseBody()
        {
        }

        [SetsRequiredMembers]
        public ResourceResponseBody(string type, JsonObject properties, string? referenceId = null, string? status = null)
        {
            this.Type = type;
            this.Properties = properties;
            this.ReferenceId = referenceId;
            this.Status = status;
        }

        public required string Type { get; init; }

        public required JsonObject Properties { get; init; }

        public string? ReferenceId { get; init; }

        public string? Status { get; init; }
    }
}
