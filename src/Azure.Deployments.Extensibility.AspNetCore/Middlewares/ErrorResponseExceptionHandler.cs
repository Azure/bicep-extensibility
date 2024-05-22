// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using System.Net;

namespace Azure.Deployments.Extensibility.AspNetCore.Middlewares
{
    public class ErrorResponseExceptionHandler : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            if (exception is ErrorResponseException errorResponseException)
            {
                await this.HandleErrorResponseException(httpContext, errorResponseException);

                return true;
            }

            return false;
        }

        protected virtual async Task HandleErrorResponseException(HttpContext httpContext, ErrorResponseException errorResponseException)
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await httpContext.Response.WriteAsJsonAsync(errorResponseException.ToErrorData());
        }
    }
}
