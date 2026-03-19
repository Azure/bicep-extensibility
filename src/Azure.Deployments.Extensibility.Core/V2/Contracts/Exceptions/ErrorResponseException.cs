// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using Azure.Deployments.Extensibility.Core.V2.Json;
using Json.Pointer;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Core.V2.Contracts.Exceptions;

/// <summary>
/// An exception that wraps an <see cref="Error"/> to propagate error information.
/// </summary>
public class ErrorResponseException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorResponseException"/> class with the specified error details.
    /// </summary>
    /// <param name="code">A service-defined error code that classifies the error.</param>
    /// <param name="message">A human-readable description of the error.</param>
    /// <param name="target">The target of the error as a JSON Pointer.</param>
    /// <param name="details">An array of additional error details.</param>
    /// <param name="innerError">An object containing more specific information about the error.</param>
    public ErrorResponseException(string code, string message, JsonPointerProxy? target = null, IList<ErrorDetail>? details = null, JsonObject? innerError = null)
        : this(new Error(code, message, target, details, innerError))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorResponseException"/> class with the specified <see cref="Models.Error"/>.
    /// </summary>
    /// <param name="error">The error object containing detailed error information.</param>
    public ErrorResponseException(Error error)
        : base(error.Message)
    {
        this.Error = error;
    }

    /// <summary>
    /// Gets the failure object containing detailed error information.
    /// </summary>
    public Error Error { get; }

    public virtual ErrorResponse ToErrorResponse() => new(this.Error);
}
