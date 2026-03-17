// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Azure.Deployments.Extensibility.AspNetCore.Middlewares;

/// <summary>
/// Middleware that reads correlation headers from the request, adds them to the logging scope,
/// and echoes the client request ID as the response <c>x-ms-request-id</c> header.
/// </summary>
public partial class RequestCorrelationMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger<RequestCorrelationMiddleware> logger;

    public RequestCorrelationMiddleware(RequestDelegate next, ILogger<RequestCorrelationMiddleware> logger)
    {
        this.next = next;
        this.logger = logger;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        var clientRequestId = httpContext.GetClientRequestId();
        var correlationRequestId = httpContext.GetCorrelationRequestId();
        var clientTenantId = httpContext.GetClientTenantId();
        var homeTenantId = httpContext.GetHomeTenantId();

        httpContext.Response.OnStarting(() =>
        {
            httpContext.Response.Headers.TryAdd(ResponseHeaderNames.RequestId, clientRequestId);

            return Task.CompletedTask;
        });

        var loggingState = new Dictionary<string, object?>
        {
            ["clientRequestId"] = clientRequestId,
            ["correlationRequestId"] = correlationRequestId,
        };
        
        if (!string.IsNullOrEmpty(clientTenantId))
        {
            loggingState["clientTenantId"] = clientTenantId;
        }
        
        if (!string.IsNullOrEmpty(homeTenantId))
        {
            loggingState["homeTenantId"] = homeTenantId;
        }

        using (this.logger.BeginScope(loggingState))
        {
            await this.next(httpContext);
        }
    }
}
