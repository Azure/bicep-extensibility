// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.ExceptionHandlers;
using Azure.Deployments.Extensibility.Core.V2.Json;
using Azure.Deployments.Extensibility.Core.V2.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Azure.Deployments.Extensibility.AspNetCore.Extensions;

/// <summary>
/// Internal extension methods for configuring extensibility infrastructure on <see cref="WebApplicationBuilder"/>.
/// </summary>
internal static class WebApplicationBuilderExtensions
{
    /// <summary>
    /// Registers extensibility infrastructure services: JSON serialization defaults,
    /// exception handling, and problem details formatting.
    /// </summary>
    public static WebApplicationBuilder AddExtensionInfrastructure(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<JsonOptions>(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, JsonDefaults.SerializerContext);
            options.SerializerOptions.PropertyNamingPolicy = JsonDefaults.SerializerOptions.PropertyNamingPolicy;
            options.SerializerOptions.DictionaryKeyPolicy = JsonDefaults.SerializerOptions.DictionaryKeyPolicy;
            options.SerializerOptions.DefaultIgnoreCondition = JsonDefaults.SerializerOptions.DefaultIgnoreCondition;
            options.SerializerOptions.Encoder = JsonDefaults.SerializerOptions.Encoder;
        });

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddExceptionHandler<DefaultExceptionHandler>();
        builder.Services.AddExtensibilityApiCompliantProblemDetails();

        return builder;
    }

    private static IServiceCollection AddExtensibilityApiCompliantProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                if (context.ProblemDetails.Extensions.ContainsKey("error"))
                {
                    // Avoid double-wrapping if already done.
                    return;
                }

                var title = context.ProblemDetails.Title;
                var detail = context.ProblemDetails.Detail;
                var status = context.ProblemDetails.Status;

                context.ProblemDetails.Extensions.Clear();
                context.ProblemDetails.Type = null;
                context.ProblemDetails.Title = null;
                context.ProblemDetails.Detail = null;
                context.ProblemDetails.Instance = null;

                // set HTTP status before clearing it from problem details
                context.HttpContext.Response.StatusCode = status ?? StatusCodes.Status500InternalServerError;
                context.ProblemDetails.Status = null;

                var errorObj = new Dictionary<string, object>
                {
                    ["code"] = !string.IsNullOrWhiteSpace(title)
                        ? title
                        : status?.ToString() ?? "UnknownProblem",
                    ["message"] = detail ?? title ?? "An unknown problem occurred."
                };

                context.ProblemDetails.Extensions["error"] = errorObj;
            };
        });

        return services;
    }
}
