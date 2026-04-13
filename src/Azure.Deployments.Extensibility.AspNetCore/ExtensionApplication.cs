// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Builders;
using Azure.Deployments.Extensibility.AspNetCore.Extensions;
using Azure.Deployments.Extensibility.AspNetCore.Behaviors;
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
    private readonly HandlerBehaviorRegistry decoratorRegistry = new();

    private ScalarApiExplorerBuilder? apiExplorerBuilder;
    private WebApplication? webApp;
    private Action<WebApplication>? pipelineConfigurator;

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
    /// Registers a global handler behavior that wraps every handler,
    /// regardless of version range or resource type.
    /// The behavior is resolved from the DI container as a scoped service per request.
    /// </summary>
    public ExtensionApplication AddGlobalHandlerBehavior<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] TBehavior>()
        where TBehavior : class
    {
        this.Builder.Services.TryAddScoped<TBehavior>();
        this.decoratorRegistry.AddGlobal(sp => sp.GetRequiredService<TBehavior>());

        return this;
    }

    /// <summary>
    /// Registers a global handler behavior using a factory that wraps every handler,
    /// regardless of version range or resource type.
    /// The factory is invoked on every request; the returned instance is not managed by the DI container.
    /// Use this overload when the behavior requires constructor arguments not available in DI.
    /// </summary>
    public ExtensionApplication AddGlobalHandlerBehavior<TBehavior>(Func<IServiceProvider, TBehavior> factory)
        where TBehavior : class
    {
        this.decoratorRegistry.AddGlobal(sp => factory(sp));

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
    public ExtensionApplication ConfigurePipeline(Action<WebApplication> configure)
    {
        this.pipelineConfigurator += configure;
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

    /// <summary>
    /// Builds the application and returns the configured <see cref="WebApplication"/>.
    /// Use this when you need to control the application lifecycle yourself,
    /// for example when hosting inside Service Fabric with
    /// <c>KestrelCommunicationListener</c>.
    /// </summary>
    public WebApplication Build()
    {
        if (this.webApp is not null)
        {
            throw new InvalidOperationException("ExtensionApplication has already been built.");
        }

        this.handlerRegistry.Validate();
        this.RegisterDefaultServices();

        var app = this.Builder.Build();

        app.UseExtensionPipeline();
        this.pipelineConfigurator?.Invoke(app);

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

        // Built-in exception-handling behavior runs as the outermost behavior.
        services.TryAddScoped<ErrorResponseExceptionHandlingBehavior>();
        this.decoratorRegistry.AddGlobal(sp => sp.GetRequiredService<ErrorResponseExceptionHandlingBehavior>());

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
