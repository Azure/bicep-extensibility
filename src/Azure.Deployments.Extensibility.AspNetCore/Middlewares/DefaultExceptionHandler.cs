// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts.Exceptions;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Azure.Deployments.Extensibility.AspNetCore.ExceptionHandlers;

internal partial class DefaultExceptionHandler : IExceptionHandler
{
    private readonly ILogger<DefaultExceptionHandler> logger;

    public DefaultExceptionHandler(ILogger<DefaultExceptionHandler> logger)
    {
        this.logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        await this.HandleUnexpectedException(httpContext, exception);

        return true;
    }

    private async Task HandleUnexpectedException(HttpContext httpContext, Exception exception)
    {
        this.LogUnexpectedException(exception);

        var internalServerErrorResult = new ErrorResponse
        {
            Error = new Error
            {
                Code = "InternalServerError",
                Message = "An unexpected error occurred",
            }
        };

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(internalServerErrorResult);
    }

    [LoggerMessage(EventId = 0xFFFF, Level = LogLevel.Error, Message = "An unexpected error occurred.")]
    private partial void LogUnexpectedException(Exception exception);
}
