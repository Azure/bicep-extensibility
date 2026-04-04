// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Deployments.Extensibility.AspNetCore.Constants
{
    /// <summary>
    /// Constants for extensibility API request header names.
    /// </summary>
    public static class RequestHeaderNames
    {
        /// <summary>The <c>x-ms-client-request-id</c> header name.</summary>
        public const string ClientRequestId = "x-ms-client-request-id";

        /// <summary>The <c>x-ms-correlation-request-id</c> header name.</summary>
        public const string CorrelationRequestId = "x-ms-correlation-request-id";

        /// <summary>The <c>x-ms-home-tenant-id</c> header name.</summary>
        public const string HomeTenantId = "x-ms-home-tenant-id";

        /// <summary>The <c>x-ms-client-tenant-id</c> header name.</summary>
        public const string ClientTenantId = "x-ms-client-tenant-id";
    }
}
