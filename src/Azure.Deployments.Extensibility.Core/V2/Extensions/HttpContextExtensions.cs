// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Azure.Deployments.Extensibility.Core.V2.Extensions
{
    public static class HttpContextExtensions
    {
        public static JsonSerializerOptions GetDefaultJsonSerializerOptions(this HttpContext httpContext) =>
            httpContext.RequestServices.GetRequiredService<IOptions<JsonSerializerOptions>>().Value;
    }
}
