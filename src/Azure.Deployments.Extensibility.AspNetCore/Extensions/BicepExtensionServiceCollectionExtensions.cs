// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Behaviors;
using Azure.Deployments.Extensibility.AspNetCore.ExceptionHandlers;
using Azure.Deployments.Extensibility.AspNetCore.Handlers;
using Azure.Deployments.Extensibility.Core.V2.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering the shared Bicep extension runtime.
/// </summary>
public static class BicepExtensionServiceCollectionExtensions
{
    /// <summary>
    /// Configures the JSON serialization defaults required by Bicep extension handlers.
    /// </summary>
    public static IServiceCollection AddBicepExtensionJsonOptions(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Configure<JsonOptions>(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, JsonDefaults.SerializerContext);
            options.SerializerOptions.PropertyNamingPolicy = JsonDefaults.SerializerOptions.PropertyNamingPolicy;
            options.SerializerOptions.DictionaryKeyPolicy = JsonDefaults.SerializerOptions.DictionaryKeyPolicy;
            options.SerializerOptions.DefaultIgnoreCondition = JsonDefaults.SerializerOptions.DefaultIgnoreCondition;
            options.SerializerOptions.Encoder = JsonDefaults.SerializerOptions.Encoder;
        });

        return services;
    }

    /// <summary>
    /// Registers the default exception handler for unexpected exceptions.
    /// </summary>
    public static IServiceCollection AddBicepExtensionExceptionHandler(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddExceptionHandler<DefaultExceptionHandler>();
        return services;
    }

    /// <summary>
    /// Registers problem details formatting that conforms to the Bicep extensibility API contract.
    /// </summary>
    public static IServiceCollection AddBicepExtensionProblemDetails(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                if (context.ProblemDetails.Extensions.ContainsKey("error"))
                {
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

                context.HttpContext.Response.StatusCode = status ?? StatusCodes.Status500InternalServerError;
                context.ProblemDetails.Status = null;
                context.ProblemDetails.Extensions["error"] = new Dictionary<string, object>
                {
                    ["code"] = !string.IsNullOrWhiteSpace(title)
                        ? title
                        : status?.ToString() ?? "UnknownProblem",
                    ["message"] = detail ?? title ?? "An unknown problem occurred.",
                };
            };
        });

        return services;
    }

    /// <summary>
    /// Registers the shared handler invocation runtime.
    /// </summary>
    public static IServiceCollection AddBicepExtensionHandlerRuntime(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<BicepExtensionRuntimeOptions>();
        services.TryAddScoped<HandlerInvoker>();

        return services;
    }

    /// <summary>
    /// Registers the shared services used by a Bicep extension host.
    /// </summary>
    public static IServiceCollection AddBicepExtensionServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddBicepExtensionJsonOptions();
        services.AddBicepExtensionExceptionHandler();
        services.AddBicepExtensionProblemDetails();
        services.AddBicepExtensionHandlerRuntime();
        services.AddHttpContextAccessor();

        return services;
    }

    /// <summary>
    /// Registers a behavior that wraps every handler invocation, including unsupported versions.
    /// </summary>
    public static IServiceCollection AddBicepExtensionGlobalHandlerBehavior<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] TBehavior>(
        this IServiceCollection services)
        where TBehavior : class
    {
        ArgumentNullException.ThrowIfNull(services);
        ValidateBehavior<TBehavior>();

        services.TryAddScoped<TBehavior>();
        AddGlobalBehavior(services, new ComponentRegistration(typeof(TBehavior)));

        return services;
    }

    /// <summary>
    /// Registers a behavior factory that wraps every handler invocation, including unsupported versions.
    /// </summary>
    public static IServiceCollection AddBicepExtensionGlobalHandlerBehavior<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] TBehavior>(
        this IServiceCollection services,
        Func<IServiceProvider, TBehavior> factory)
        where TBehavior : class
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(factory);
        ValidateBehavior<TBehavior>();

        var serviceKey = new object();
        services.AddKeyedScoped<TBehavior>(serviceKey, (serviceProvider, _) => factory(serviceProvider));
        AddGlobalBehavior(services, new ComponentRegistration(typeof(TBehavior), serviceKey));

        return services;
    }

    private static void AddGlobalBehavior(IServiceCollection services, ComponentRegistration registration)
    {
        services.AddOptions<BicepExtensionRuntimeOptions>()
            .Configure(options => options.GlobalBehaviors.Add(registration));
    }

    private static void ValidateBehavior<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] TBehavior>()
        where TBehavior : class
    {
        if (!HandlerContractTypes.IsSupportedBehavior(typeof(TBehavior)))
        {
            throw new InvalidOperationException(
                $"Behavior type '{typeof(TBehavior)}' does not implement a supported handler behavior interface.");
        }
    }
}
