// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core;
using Azure.Deployments.Extensibility.Core.Exceptions;

namespace Azure.Deployments.Extensibility.DevHost.Middlewares
{
    public class ExtensibilityExceptionHandlingMiddleware
    {
        private readonly RequestDelegate next;

        public ExtensibilityExceptionHandlingMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await this.next.Invoke(context);
            }
            catch (ExtensibilityException exception)
            {
                var errorResponse = new ExtensibilityOperationErrorResponse(exception.Errors);

                await context.Response.WriteAsJsonAsync(errorResponse);
            }
        }
    }
}
