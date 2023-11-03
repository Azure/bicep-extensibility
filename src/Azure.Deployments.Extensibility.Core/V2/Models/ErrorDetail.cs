// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Json.Pointer;
using System.Diagnostics.CodeAnalysis;

namespace Azure.Deployments.Extensibility.Core.V2.Models
{
    public record ErrorDetail
    {
        public ErrorDetail()
        {
        }

        [SetsRequiredMembers]
        public ErrorDetail(string code, string message, JsonPointer? target = null)
        {
            this.Code = code;
            this.Message = message;
            this.Target = target;
        }

        public required string Code { get; init; }

        public required string Message { get; init; }

        public JsonPointer? Target { get; init; }

        public Error AsError() => new(this.Code, this.Message, this.Target);
    }
}
