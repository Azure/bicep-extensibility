// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Builders;
using Azure.Deployments.Extensibility.AspNetCore.Extensions;
using Azure.Deployments.Extensibility.AspNetCore.Handlers;
using Azure.Deployments.Extensibility.AspNetCore.Pipeline;
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
/// <code>
/// ExtensionApplication.Create(args)
///     .AddExtensionVersion("&gt;=1.0.0", v =&gt; v
///         .ForResourceType("apps/Deployment", rt =&gt; rt
///             .AddHandler&lt;DeploymentHandler&gt;())
///         .AddHandler&lt;FallbackHandler&gt;())
///     .Run();
/// </code>
/// </example>
public class ExtensionApplication
{
    private readonly WebApplicationBuilder webApplicationBuilder;
    private readonly HandlerRegistry handlerRegistry = new();
    private readonly HandlerPipelineRegistry pipelineRegistry = new();
    private Action<OpenApiExamplesBuilder>? apiExplorerConfigurator;
    private string? apiExplorerTitle;
    private string[]? apiExplorerExtensionVersions;
    private Action<WebApplication>? middlewareConfigurator;

    private ExtensionApplication(string[] args)
    {
        this.webApplicationBuilder = WebApplication.CreateBuilder(args);
    }

    /// <summary>
    /// Creates a new <see cref="ExtensionApplication"/> instance.
    /// </summary>
    public static ExtensionApplication Create(string[] args) => new(args);

    /// <summary>
    /// The underlying <see cref="WebApplicationBuilder"/> for advanced configuration.
    /// </summary>
    public WebApplicationBuilder Builder => this.webApplicationBuilder;

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
            this.webApplicationBuilder.Services,
            this.handlerRegistry,
            this.pipelineRegistry,
            range));

        this.handlerRegistry.TrackVersionRange(range);

        return this;
    }

    /// <summary>
    /// Registers a global pipeline behavior that wraps every handler,
    /// regardless of version range or resource type.
    /// </summary>
    public ExtensionApplication AddGlobalPipelineBehavior<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] TBehavior>()
        where TBehavior : class
    {
        this.webApplicationBuilder.Services.TryAddScoped<TBehavior>();
        this.pipelineRegistry.AddGlobal(typeof(TBehavior));

        return this;
    }

    /// <summary>
    /// Registers additional services with the dependency injection container.
    /// </summary>
    public ExtensionApplication ConfigureServices(Action<IServiceCollection> configure)
    {
        configure(this.webApplicationBuilder.Services);
        return this;
    }

    /// <summary>
    /// Configures custom middlewares on the application pipeline.
    /// The callback is invoked after the built-in middlewares but before endpoint routing.
    /// </summary>
    public ExtensionApplication ConfigureMiddlewares(Action<WebApplication> configure)
    {
        this.middlewareConfigurator = configure;
        return this;
    }

    /// <summary>
    /// Enables the Scalar API explorer with optional request/response examples.
    /// Only served in the Development environment.
    /// </summary>
    public ExtensionApplication EnableDevelopmentScalarApiExplorer(Action<OpenApiExamplesBuilder>? configureExamples = null, string? title = null, string[]? extensionVersions = null)
    {
        // Use a no-op delegate to distinguish "enabled without examples" from "not enabled".
        this.apiExplorerConfigurator = configureExamples ?? (_ => { });
        this.apiExplorerTitle = title;
        this.apiExplorerExtensionVersions = extensionVersions;
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
        this.handlerRegistry.Validate();
        this.RegisterDefaultServices();

        var app = this.webApplicationBuilder.Build();

        app.UseExtensionApplicationMiddlewares();
        this.middlewareConfigurator?.Invoke(app);

        this.MapEndpoints(app);

        return app;
    }

    private void RegisterDefaultServices()
    {
        var services = this.webApplicationBuilder.Services;

        // Built-in exception-handling behavior runs as the outermost pipeline layer.
        services.TryAddScoped<ErrorResponseExceptionHandlingBehavior>();
        this.pipelineRegistry.AddGlobal(typeof(ErrorResponseExceptionHandlingBehavior));

        services.AddSingleton(this.handlerRegistry);
        services.AddSingleton(this.pipelineRegistry);

        this.webApplicationBuilder.AddExtensionInfrastructure();
    }

    private void MapEndpoints(WebApplication app)
    {
        if (this.apiExplorerConfigurator is not null)
        {
            app.MapDevelopmentScalarApiExplorer(
                this.apiExplorerConfigurator,
                this.apiExplorerTitle ?? ScalarExtensions.DefaultTitle,
                this.apiExplorerExtensionVersions);
        }

        app.MapResourceActions();

        if (this.handlerRegistry.HasRegistration(typeof(ILongRunningOperationGetHandler)))
        {
            app.MapLongRunningOperationActions();
        }
    }
}
