// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Middlewares;
using Microsoft.AspNetCore.Builder;

namespace Azure.Deployments.Extensibility.AspNetCore.Extensions
{
    public static class IApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseRequestCorrelationContext(this IApplicationBuilder builder) =>
            builder.UseMiddleware<RequestCorrelationContextMiddleware>();

        public static IApplicationBuilder UseRequestCulture(this IApplicationBuilder builder) =>
            builder.UseMiddleware<RequestCultureMiddleware>();
    }
}
