// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Azure.Deployments.Extensibility.Hosting.Managed;

/// <summary>
/// In the Development environment only, back-fills the required correlation headers
/// (x-ms-client-request-id, x-ms-correlation-request-id) when they are missing, so local
/// callers (curl, bicep local-deploy) are not rejected by the correlation middleware.
/// Production traffic always carries these headers, so this filter does nothing there.
/// </summary>
internal sealed class DevelopmentHeaderBackfillStartupFilter : IStartupFilter
{
    private static readonly string[] RequiredHeaders =
    [
        "x-ms-client-request-id",
        "x-ms-correlation-request-id",
    ];

    private readonly IWebHostEnvironment environment;

    public DevelopmentHeaderBackfillStartupFilter(IWebHostEnvironment environment) =>
        this.environment = environment;

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        if (this.environment.IsDevelopment())
        {
            app.Use(async (context, nextMiddleware) =>
            {
                foreach (var header in RequiredHeaders)
                {
                    if (!context.Request.Headers.ContainsKey(header))
                    {
                        context.Request.Headers[header] = Guid.NewGuid().ToString();
                    }
                }

                await nextMiddleware();
            });
        }

        next(app);
    };
}
