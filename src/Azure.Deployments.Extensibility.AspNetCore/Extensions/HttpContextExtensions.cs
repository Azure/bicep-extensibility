// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Constants;
using Microsoft.AspNetCore.Http;
using System.Runtime.CompilerServices;

namespace Azure.Deployments.Extensibility.AspNetCore.Extensions
{
    internal static class HttpContextExtensions
    {
        public static void SetClientAppId(this HttpContext httpContext) =>
            httpContext.SetRequestCorrelationHeaderItem(RequestHeaderNames.ClientAppId, nameof(RequestHeaderNames.ClientAppId));

        public static void SetClientRequestId(this HttpContext httpContext) =>
            httpContext.SetRequestCorrelationHeaderItem(RequestHeaderNames.ClientRequestId, nameof(RequestHeaderNames.ClientRequestId));

        public static void SetCorrelationRequestId(this HttpContext httpContext) =>
            httpContext.SetRequestCorrelationHeaderItem(RequestHeaderNames.CorrelationRequestId, nameof(RequestHeaderNames.CorrelationRequestId));

        public static void SetRequestId(this HttpContext httpContext)
        {
            var requestId = Guid.NewGuid().ToString();
            httpContext.Response.Headers[ResponseHeaderNames.RequestId] = requestId;
            httpContext.Items[nameof(ResponseHeaderNames.RequestId)] = requestId;
        }

        public static string GetClientAppId(this HttpContext httpContext) =>
            httpContext.GetCorrelationHeaderItem(nameof(RequestHeaderNames.ClientAppId));

        public static string GetClientRequestId(this HttpContext httpContext) =>
            httpContext.GetCorrelationHeaderItem(nameof(RequestHeaderNames.ClientRequestId));

        public static string GetCorrelationRequestId(this HttpContext httpContext) =>
            httpContext.GetCorrelationHeaderItem(nameof(RequestHeaderNames.CorrelationRequestId));

        public static string GetRequestId(this HttpContext httpContext) =>
            httpContext.GetCorrelationHeaderItem(nameof(ResponseHeaderNames.RequestId));

        private static void SetRequestCorrelationHeaderItem(this HttpContext httpContext, string headerName, string itemName)
        {
            if (httpContext.Request.Headers.TryGetValue(headerName, out var value))
            {
                if (Guid.TryParse(value, out var clientRequestId))
                {
                    httpContext.Items[itemName] = clientRequestId.ToString();
                }
                else
                {
                    httpContext.Items[itemName] = $"<invalid: {value}>";
                }
            }
            else
            {
                httpContext.Items[itemName] = "<missing>";
            }
        }

        private static string GetCorrelationHeaderItem(this HttpContext httpContext, string itemName, [CallerMemberName] string callerMemberName = "") =>
            httpContext.Items[itemName] as string ??
            throw new InvalidOperationException($"The HttpContext item {itemName} is not set. Make sure ${nameof(HttpContextExtensions)}.{callerMemberName.Replace("Get", "Set")} is invoked before calling this method.");
    }
}
