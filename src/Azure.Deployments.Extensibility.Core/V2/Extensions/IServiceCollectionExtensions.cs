// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Azure.Deployments.Extensibility.Core.V2.Extensions
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddDefaultJsonSerializerOptions(this IServiceCollection services)
        {
            services.Configure<JsonOptions>(options =>
            {
                options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                options.SerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
            });

            return services;
        }
    }
}
