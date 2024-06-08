// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http;
using System.Globalization;

namespace Azure.Deployments.Extensibility.AspNetCore.Middlewares
{
    public class RequestCultureMiddleware
    {
        private readonly static CultureInfo DefaultCulture = new("en-US");

        private readonly RequestDelegate next;

        public RequestCultureMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            var culture = httpContext.Request.Headers.AcceptLanguage.SingleOrDefault() is string acceptLanguage
                ? SafeCreateCulture(acceptLanguage)
                : DefaultCulture;

            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            await this.next(httpContext);
        }

        private static CultureInfo SafeCreateCulture(string name)
        {
            try
            {
                return CultureInfo.CreateSpecificCulture(name);
            }
            catch (CultureNotFoundException)
            {
                return DefaultCulture;
            }
        }
    }
}
