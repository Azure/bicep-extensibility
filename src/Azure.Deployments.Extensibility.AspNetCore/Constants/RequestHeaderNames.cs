// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Azure.Deployments.Extensibility.AspNetCore.Constants
{
    /// <summary>
    /// Constants for extensibility API request header names.
    /// </summary>
    public static class RequestHeaderNames
    {
        public const string ClientRequestId = "x-ms-client-request-id";

        public const string CorrelationRequestId = "x-ms-correlation-request-id";

        public const string HomeTenantId = "x-ms-home-tenant-id";

        public const string ClientTenantId = "x-ms-client-tenant-id";
    }
}
