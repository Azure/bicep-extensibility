// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Models;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Exceptions;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using Json.Pointer;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.AspNetCore.Exceptions;

/// <summary>
/// An exception that wraps an <see cref="Error"/> to propagate error information.
/// </summary>
public class HttpErrorResponseException : ErrorResponseException
{
    public HttpErrorResponseException(int statusCode, string code, string message, JsonPointer? target = null, IList<ErrorDetail>? details = null, JsonObject? innerError = null)
        : this(statusCode, new Error(code, message, target, details, innerError))
    {
    }

    public HttpErrorResponseException(int statusCode, Error error)
        : base(error)
    {
        this.StatusCode = statusCode;
    }
    
    public int StatusCode { get; }

    /// <inheritdoc />
    public override ErrorResponse ToErrorResponse() => new HttpErrorResponse(this.StatusCode, this.Error);
}
