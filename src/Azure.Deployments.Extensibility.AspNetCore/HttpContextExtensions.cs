// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Constants;
using Microsoft.AspNetCore.Http;
using System.Runtime.CompilerServices;

namespace Azure.Deployments.Extensibility.AspNetCore;

/// <summary>
/// Extension methods for reading extensibility request headers from <see cref="HttpContext"/>.
/// </summary>
public static class HttpContextExtensions
{
    /// <summary>
    /// Get the client request ID from the <c>x-ms-client-request-id</c> header.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the header is missing.</exception>
    public static string GetClientRequestId(this HttpContext httpContext) =>
        httpContext.GetRequiredHeaderValue(RequestHeaderNames.ClientRequestId);

    /// <summary>
    /// Get the correlation request ID from the <c>x-ms-correlation-request-id</c> header.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the header is missing.</exception>
    public static string GetCorrelationRequestId(this HttpContext httpContext) =>
        httpContext.GetRequiredHeaderValue(RequestHeaderNames.CorrelationRequestId);

    /// <summary>
    /// Get the client tenant ID from the <c>x-ms-client-tenant-id</c> header, or <see langword="null"/> if not present.
    /// </summary>
    public static string? GetClientTenantId(this HttpContext httpContext) =>
        httpContext.TryGetHeaderValue(RequestHeaderNames.ClientTenantId);

    /// <summary>
    /// Get the home tenant ID from the <c>x-ms-home-tenant-id</c> header, or <see langword="null"/> if not present.
    /// </summary>
    public static string? GetHomeTenantId(this HttpContext httpContext) =>
        httpContext.TryGetHeaderValue(RequestHeaderNames.HomeTenantId);

    private static string GetRequiredHeaderValue(this HttpContext httpContext, string headerName) =>
        httpContext.TryGetHeaderValue(headerName) ?? throw new InvalidOperationException($"Required header '{headerName}' is missing from the request.");

    private static string? TryGetHeaderValue(this HttpContext httpContext, string headerName) =>
        httpContext.Request.Headers.TryGetValue(headerName, out var headerValue)
            ? headerValue.ToString()
            : null;
}
