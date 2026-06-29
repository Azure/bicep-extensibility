// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace Azure.Deployments.Extensibility.Hosting.Managed;

/// <summary>
/// Handles the required correlation headers (x-ms-client-request-id, x-ms-correlation-request-id)
/// before the correlation middleware runs:
/// <list type="bullet">
/// <item>In Development, missing headers are back-filled with a generated value so local callers
/// (curl, bicep local-deploy) are not rejected.</item>
/// <item>In other environments, a missing header on a correlated route yields a structured
/// 400 Bad Request instead of letting the correlation middleware throw (which would surface as a 500).
/// The platform normally supplies these headers, so this only affects direct callers.</item>
/// </list>
/// </summary>
internal sealed class CorrelationHeaderStartupFilter : IStartupFilter
{
    private static readonly string[] RequiredHeaders =
    [
        "x-ms-client-request-id",
        "x-ms-correlation-request-id",
    ];

    private readonly IWebHostEnvironment environment;

    public CorrelationHeaderStartupFilter(IWebHostEnvironment environment) =>
        this.environment = environment;

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use(async (context, nextMiddleware) =>
        {
            foreach (var header in RequiredHeaders)
            {
                if (context.Request.Headers.ContainsKey(header))
                {
                    continue;
                }

                if (this.environment.IsDevelopment())
                {
                    context.Request.Headers[header] = Guid.NewGuid().ToString();
                }
                else if (RequiresCorrelationHeaders(context.Request.Path))
                {
                    await WriteMissingHeaderResponse(context, header);
                    return;
                }
            }

            await nextMiddleware();
        });

        next(app);
    };

    private static bool RequiresCorrelationHeaders(PathString path) =>
        path.HasValue &&
        (path.Value!.Contains("/resource/", StringComparison.OrdinalIgnoreCase) ||
         path.Value!.Contains("/longRunningOperation/", StringComparison.OrdinalIgnoreCase));

    private static Task WriteMissingHeaderResponse(HttpContext context, string headerName)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;

        return context.Response.WriteAsJsonAsync(new ErrorResponse
        {
            Error = new Error
            {
                Code = "MissingRequiredHeader",
                Message = $"Required header '{headerName}' is missing from the request.",
            }
        });
    }
}
