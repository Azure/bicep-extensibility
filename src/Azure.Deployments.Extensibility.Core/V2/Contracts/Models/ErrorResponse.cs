// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace Azure.Deployments.Extensibility.Core.V2.Contracts.Models;

/// <summary>
/// A wrapper for <see cref="Error"/> to be used as extensibility
/// API error response.
/// </summary>
public record ErrorResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorResponse"/> record.
    /// </summary>
    public ErrorResponse()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorResponse"/> record with the specified error.
    /// </summary>
    /// <param name="error">The error object containing detailed error information.</param>
    [SetsRequiredMembers]
    public ErrorResponse(Error error)
    {
        this.Error = error;
    }

    /// <summary>
    /// The error object containing detailed error information.
    /// </summary>
    public required Error Error { get; init; }
}
