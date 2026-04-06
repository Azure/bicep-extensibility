// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;

namespace Azure.Deployments.Extensibility.AspNetCore.Models;

/// <summary>
/// An <see cref="ErrorResponse"/> that includes an HTTP status code.
/// </summary>
public record HttpErrorResponse : ErrorResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HttpErrorResponse"/> record.
    /// </summary>
    public HttpErrorResponse()
    {
    }

    /// <summary>
    /// Initializes a new instance with the specified HTTP status code and error.
    /// </summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="error">The error object.</param>
    [SetsRequiredMembers]
    public HttpErrorResponse(int statusCode, Error error)
        : base(error)
    {
        this.StatusCode = statusCode;
    }

    /// <summary>
    /// Gets the HTTP status code associated with this error response.
    /// </summary>
    public int StatusCode { get; }
    
    /// <summary>
    /// Convert this <see cref="HttpErrorResponse"/> to a plain <see cref="ErrorResponse"/>.
    /// </summary>
    /// <returns>An <see cref="ErrorResponse"/> with the same <see cref="ErrorResponse.Error"/>.</returns>
    public ErrorResponse AsErrorResponse() => new(this.Error);
}
