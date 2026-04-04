// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Models;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Exceptions;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using Azure.Deployments.Extensibility.Core.V2.Json;
using Json.Pointer;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.AspNetCore.Exceptions;

/// <summary>
/// An exception that wraps an <see cref="Error"/> with an HTTP status code to propagate error information.
/// </summary>
public class HttpErrorResponseException : ErrorResponseException
{
    /// <summary>
    /// Initializes a new instance with the specified HTTP status code and error details.
    /// </summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="code">A service-defined error code.</param>
    /// <param name="message">A human-readable description of the error.</param>
    /// <param name="target">The JSON Pointer to the error target.</param>
    /// <param name="details">Additional error details.</param>
    /// <param name="innerError">An object containing more specific error information.</param>
    public HttpErrorResponseException(int statusCode, string code, string message, JsonPointerProxy? target = null, IList<ErrorDetail>? details = null, JsonObject? innerError = null)
        : this(statusCode, new Error(code, message, target, details, innerError))
    {
    }

    /// <summary>
    /// Initializes a new instance with the specified HTTP status code and error.
    /// </summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="error">The error object.</param>
    public HttpErrorResponseException(int statusCode, Error error)
        : base(error)
    {
        this.StatusCode = statusCode;
    }

    /// <summary>
    /// Gets the HTTP status code associated with this error.
    /// </summary>
    public int StatusCode { get; }

    /// <inheritdoc />
    public override ErrorResponse ToErrorResponse() => new HttpErrorResponse(this.StatusCode, this.Error);
}
