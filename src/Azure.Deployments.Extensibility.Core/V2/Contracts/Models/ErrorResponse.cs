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
    public ErrorResponse()
    {
    }

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
