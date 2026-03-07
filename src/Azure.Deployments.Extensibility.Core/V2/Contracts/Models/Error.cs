// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;
using Json.Pointer;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Azure.Deployments.Extensibility.Core.V2.Contracts.Models
{
    /// <summary>
    /// Error model that can be used to return rich error information.
    /// <remarks>
    /// This model is based on the OData v4 error response format, which is
    /// slighly different from the Azure Resource Manager (ARM) error response format.
    /// The ARM Template deployment service converts this format to the ARM error
    /// response format before returning it to the client to make it compatible with
    /// Azure Resource Manager clients and SDKs.
    /// </remarks>
    /// </summary>
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
        /// An array of additional error details about specific errors that led to this reported error.
        /// </summary>
        public IList<ErrorDetail>? Details { get; init; }

        /// <summary>
        /// An object containing more specific information about the error.
        /// The contents of this object are service-defined and may contain additional nested error information.
        /// </summary>
        [JsonPropertyName("innererror")]

        public JsonObject? InnerError { get; init; }
    }
}
