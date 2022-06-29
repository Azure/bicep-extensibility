// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.DevHost.Handlers;
using Azure.Deployments.Extensibility.DevHost.Middlewares;

namespace Azure.Deployments.Extensibility.DevHost.Extensions
{
    public static class WebApplicationExtensions
    {
        public static void UseExtensibilityExceptionHandler(this WebApplication app)
        {
            app.UseMiddleware<ExtensibilityExceptionHandlingMiddleware>();
        }

        public static void MapHandlers(this WebApplication app)
        {
            new SaveRequestHandler().RegisterRoute(app);
            new PreviewSaveRequestHandler().RegisterRoute(app);
            new GetRequestHandler().RegisterRoute(app);
            new DeleteRequestHandler().RegisterRoute(app);
        }
    }
}
