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
    public static string GetClientRequestId(this HttpContext httpContext) =>
        httpContext.GetRequiredHeaderValue(RequestHeaderNames.ClientRequestId);

    public static string GetCorrelationRequestId(this HttpContext httpContext) =>
        httpContext.GetRequiredHeaderValue(RequestHeaderNames.CorrelationRequestId);

    public static string? GetClientTenantId(this HttpContext httpContext) =>
        httpContext.TryGetHeaderValue(RequestHeaderNames.ClientTenantId);

    public static string? GetHomeTenantId(this HttpContext httpContext) =>
        httpContext.TryGetHeaderValue(RequestHeaderNames.HomeTenantId);

    private static string GetRequiredHeaderValue(this HttpContext httpContext, string headerName) =>
        httpContext.TryGetHeaderValue(headerName) ?? throw new InvalidOperationException($"Required header '{headerName}' is missing from the request.");

    private static string? TryGetHeaderValue(this HttpContext httpContext, string headerName) =>
        httpContext.Request.Headers.TryGetValue(headerName, out var headerValue)
            ? headerValue.ToString()
            : null;
}
