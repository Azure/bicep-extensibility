// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;

namespace Azure.Deployments.Extensibility.AspNetCore.Models;

/// <summary>
/// A wrapper for <see cref="Error"/> to be used as extensibility
/// API error response.
/// </summary>
public record HttpErrorResponse : ErrorResponse
{
    public HttpErrorResponse()
    {
    }

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
    
    public ErrorResponse AsErrorResponse() => new(this.Error);
}
