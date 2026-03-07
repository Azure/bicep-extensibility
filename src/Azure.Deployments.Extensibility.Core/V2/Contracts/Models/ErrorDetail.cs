// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Json.Pointer;
using System.Diagnostics.CodeAnalysis;

namespace Azure.Deployments.Extensibility.Core.V2.Contracts.Models
{
    /// <summary>
    /// Represents detailed information about an error, including a code, message, and an optional target reference.
    /// </summary>
    public record ErrorDetail
    {
        public ErrorDetail()
        {
        }

        [SetsRequiredMembers]
        public ErrorDetail(string code, string message, JsonPointer? target)
        {
            this.Code = code;
            this.Message = message;
            this.Target = target;
        }

        /// <summary>
        /// A service-defined error code that classifies the error.
        /// </summary>
        public required string Code { get; init; }

        /// <summary>
        /// A human-readable description of the error.
        /// </summary>
        public required string Message { get; init; }

        /// <summary>
        /// The target of the error. This should be a JSON Pointer that indicates the location in the request payload that caused the error.
        /// </summary>
        public JsonPointer? Target { get; init; }

        /// <summary>
        /// Converts this <see cref="ErrorDetail"/> to an <see cref="Error"/> instance.
        /// </summary>
        /// <returns>An <see cref="Error"/> with the same <see cref="Code"/>, <see cref="Message"/>, and <see cref="Target"/> values.</returns>
        public Error ToError() => new()
        {
            Code = this.Code,
            Message = this.Message,
            Target = this.Target,
        };
    }
}
