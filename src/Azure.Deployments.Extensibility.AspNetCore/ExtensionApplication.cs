// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Builders;
using Azure.Deployments.Extensibility.AspNetCore.Extensions;
using Azure.Deployments.Extensibility.AspNetCore.Decorators;
using Azure.Deployments.Extensibility.AspNetCore.Handlers;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Semver;
using System.Diagnostics.CodeAnalysis;

namespace Azure.Deployments.Extensibility.AspNetCore;

/// <summary>
/// The entry point for building a Bicep extensibility provider application.
/// Provides a fluent API that handles JSON serialization, middleware, routing,
/// and pipeline behavior configuration automatically.
/// </summary>
/// <example>
/// <code><![CDATA[
/// ExtensionApplication.Create(args)
///     .AddExtensionVersion(">=1.0.0", v => v
///         .ForResourceType("apps/Deployment", rt => rt
///             .AddHandler<DeploymentHandler>())
///         .AddHandler<FallbackHandler>())
///     .Run();
/// ]]></code>
/// </example>
public class ExtensionApplication
{
    private readonly HandlerRegistry handlerRegistry = new();
    private readonly HandlerDecoratorRegistry decoratorRegistry = new();

    private ScalarApiExplorerBuilder? apiExplorerBuilder;
    private WebApplication? webApp;
    private Action<WebApplication>? middlewareConfigurator;

    private ExtensionApplication(string[] args)
    {
        this.Builder = WebApplication.CreateBuilder(args);
    }

    /// <summary>
    /// Creates a new <see cref="ExtensionApplication"/> instance.
    /// </summary>
    public static ExtensionApplication Create(string[] args) => new(args);

    /// <summary>
    /// The underlying <see cref="WebApplicationBuilder"/> for advanced configuration.
    /// </summary>
    public WebApplicationBuilder Builder { get; }

    /// <summary>
    /// Registers handlers for a specific extension version range.
    /// </summary>
    /// <param name="versionRange">
    /// A semantic version range string (e.g., <c>"&gt;=1.0.0 &lt;2.0.0"</c>).
    /// </param>
    /// <param name="configure">
    /// A callback that registers handlers via
    /// <see cref="ExtensionVersionBuilder.AddHandler{THandler}"/> or
    /// <see cref="ExtensionVersionBuilder.ForResourceType"/>.
    /// </param>
    public ExtensionApplication AddExtensionVersion(string versionRange, Action<ExtensionVersionBuilder> configure)
    {
        var range = SemVersionRange.Parse(versionRange);

        configure(new ExtensionVersionBuilder(
            this.Builder.Services,
            this.handlerRegistry,
            this.decoratorRegistry,
            range));

        this.handlerRegistry.TrackVersionRange(range);

        return this;
    }

    /// <summary>
    /// Registers a global handler decorator that wraps every handler,
    /// regardless of version range or resource type.
    /// </summary>
    public ExtensionApplication AddGlobalHandlerDecorator<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] TDecorator>()
        where TDecorator : class
    {
        this.Builder.Services.TryAddScoped<TDecorator>();
        this.decoratorRegistry.AddGlobal(typeof(TDecorator));

        return this;
    }

    /// <summary>
    /// Registers additional services with the dependency injection container.
    /// </summary>
    public ExtensionApplication ConfigureServices(Action<IServiceCollection> configure)
    {
        configure(this.Builder.Services);
        return this;
    }

    /// <summary>
    /// Configures custom ASP.NET core middlewares on the application pipeline.
    /// The callback is invoked after the built-in middlewares but before endpoint routing.
    /// </summary>
    public ExtensionApplication ConfigureHttpPipeline(Action<WebApplication> configure)
    {
        this.middlewareConfigurator += configure;
        return this;
    }

    /// <summary>
    /// Enables the Scalar API explorer with optional configuration.
    /// Only served in the Development environment.
    /// </summary>
    public ExtensionApplication EnableDevelopmentScalarApiExplorer(Action<ScalarApiExplorerBuilder>? configure = null)
    {
        var builder = new ScalarApiExplorerBuilder();
        configure?.Invoke(builder);
        this.apiExplorerBuilder = builder;

        return this;
    }

    /// <summary>
    /// Builds and runs the application. Does not return until shutdown.
    /// </summary>
    public void Run() => this.Build().Run();

    /// <summary>
    /// Builds and runs the application asynchronously.
    /// </summary>
    public Task RunAsync() => this.Build().RunAsync();

    private WebApplication Build()
    {
        if (this.webApp is not null)
        {
            throw new InvalidOperationException("ExtensionApplication has already been built.");
        }

        this.handlerRegistry.Validate();
        this.RegisterDefaultServices();

        var app = this.Builder.Build();

        app.UseExtensionPipeline();
        this.middlewareConfigurator?.Invoke(app);

        this.MapExtensionEndpoints(app);

        webApp = app;
        return app;
    }

    /// <remarks>
    /// <see cref="ErrorResponseExceptionHandlingBehavior"/> is always registered
    /// as the outermost pipeline layer and cannot be removed.
    /// </remarks>
    private void RegisterDefaultServices()
    {
        var services = this.Builder.Services;

        // Built-in exception-handling decorator runs as the outermost decorator.
        services.TryAddScoped<ErrorResponseExceptionHandlingBehavior>();
        this.decoratorRegistry.AddGlobal(typeof(ErrorResponseExceptionHandlingBehavior));

        services.AddSingleton(this.handlerRegistry);
        services.AddSingleton(this.decoratorRegistry);

        this.Builder.AddExtensionInfrastructure();
    }

    private void MapExtensionEndpoints(WebApplication app)
    {
        if (this.apiExplorerBuilder is { } explorer)
        {
            app.MapDevelopmentScalarApiExplorer(
                explorer.ExamplesConfigurator,
                explorer.Title,
                explorer.ExtensionVersions);
        }

        app.MapResourceActions();

        if (this.handlerRegistry.HasRegistration(typeof(ILongRunningOperationGetHandler)))
        {
            app.MapLongRunningOperationActions();
        }
    }
}
