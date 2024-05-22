// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Extensions;
using Microsoft.AspNetCore.Http;

namespace Azure.Deployments.Extensibility.AspNetCore.Middlewares
{
    public class RequestCorrelationContextMiddleware
    {
        private readonly RequestDelegate next;

        public RequestCorrelationContextMiddleware(RequestDelegate requestDelegate)
        {
            this.next = requestDelegate;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            httpContext.SetClientAppId();
            httpContext.SetClientRequestId();
            httpContext.SetCorrelationRequestId();

            // Set x-ms-request-id response header.
            httpContext.SetRequestId();

            await this.next(httpContext);
        }
    }
}
