// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Json;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Azure.Deployments.Extensibility.AspNetCore.Extensions
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddDefaultJsonSerializerOptions(this IServiceCollection services)
        {
            services.Configure<JsonOptions>(options => DefaultJsonSerializerContext.ConfigureSerializerOptions(options.SerializerOptions));

            return services;
        }
    }
}
