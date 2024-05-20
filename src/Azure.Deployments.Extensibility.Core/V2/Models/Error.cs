// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Json.Pointer;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Azure.Deployments.Extensibility.Core.V2.Models
{
    public record Error
    {
        public Error()
        {
        }

        [SetsRequiredMembers]
        public Error(string code, string message, JsonPointer? target = null, IList<ErrorDetail>? details = null, JsonObject? innerError = null)
        {
            this.Code = code;
            this.Message = message;
            this.Target = target;
            this.Details = details;
            this.InnerError = innerError;
        }

        public required string Code { get; init; }

        public required string Message { get; init; }

        public JsonPointer? Target { get; init; }

        public IList<ErrorDetail>? Details { get; init; }

        [JsonPropertyName("innererror")]

        public JsonObject? InnerError { get; init; }
    }
}
