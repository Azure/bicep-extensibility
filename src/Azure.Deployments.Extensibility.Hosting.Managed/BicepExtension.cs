// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Semver;

namespace Azure.Deployments.Extensibility.Hosting.Managed;

/// <summary>
/// Opinionated entry point for building a Bicep extension that runs in the managed
/// (platform-hosted) model. Wraps <see cref="ExtensionApplication"/> and applies the
/// platform contract defaults (EXTENSION_PORT binding, GET /health, local-dev relaxations).
/// </summary>
public sealed class BicepExtension
{
    private const int DefaultPort = 8080;

    private readonly ExtensionApplication inner;
    private SemVersion? registeredVersion;

    private BicepExtension(string[] args)
    {
        this.inner = ExtensionApplication.Create(args);
        this.ApplyDefaults();
    }

    /// <summary>Creates a builder with the managed-hosting defaults applied.</summary>
    public static BicepExtension CreateBuilder(string[] args) => new(args);

    private void ApplyDefaults()
    {
        // (1) Bind Kestrel to EXTENSION_PORT (default 8080). ListenAnyIP so it works in a container.
        var port = ResolvePort();
        this.inner.Builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(port));

        // (2) Always register the health-check service so GET /health can be mapped at build time.
        this.inner.ConfigureServices(services => services.AddHealthChecks());

        // (3) Local-dev header relaxation (see DevelopmentHeaderBackfillStartupFilter).
        this.inner.ConfigureServices(services =>
            services.AddTransient<IStartupFilter, DevelopmentHeaderBackfillStartupFilter>());

        // (4) Structured logging on by default.
        this.inner.Builder.Logging.AddJsonConsole();
    }

    private static int ResolvePort()
    {
        var raw = Environment.GetEnvironmentVariable("EXTENSION_PORT");
        return int.TryParse(raw, out var parsed) && parsed > 0 ? parsed : DefaultPort;
    }

    /// <summary>
    /// Registers handlers for a single exact extension version. May be called only once.
    /// </summary>
    public BicepExtension AddExtensionVersion(string exactVersion, Action<ExtensionVersionBuilder> configure)
    {
        // Reject ranges/wildcards: a single exact version per binary keeps routing unambiguous.
        var version = SemVersion.Parse(exactVersion, SemVersionStyles.Strict);

        if (this.registeredVersion is not null)
        {
            throw new InvalidOperationException(
                "AddExtensionVersion may be called only once. A managed extension binary serves exactly one version.");
        }

        this.registeredVersion = version;
        this.inner.AddExtensionVersion(exactVersion, configure);
        return this;
    }

    /// <summary>Adds a readiness check that the always-mapped GET /health endpoint aggregates.</summary>
    public BicepExtension AddHealthCheck<THealthCheck>(string name)
        where THealthCheck : class, IHealthCheck
    {
        this.inner.ConfigureServices(services => services.AddHealthChecks().AddCheck<THealthCheck>(name));
        return this;
    }

    /// <summary>Passthrough for registering additional services.</summary>
    public BicepExtension ConfigureServices(Action<IServiceCollection> configure)
    {
        this.inner.ConfigureServices(configure);
        return this;
    }

    /// <summary>Builds and runs the extension. Does not return until shutdown.</summary>
    public void Run() => this.Build().Run();

    /// <summary>Builds and runs the extension asynchronously.</summary>
    public Task RunAsync() => this.Build().RunAsync();

    /// <summary>
    /// Builds the underlying application, maps GET /health, and returns the WebApplication
    /// for callers that want to control the lifecycle themselves.
    /// </summary>
    public WebApplication Build()
    {
        // Scalar must be enabled before Build(); wire it to the registered version when present.
        if (this.registeredVersion is { } version)
        {
            this.inner.EnableDevelopmentScalarApiExplorer(explorer =>
                explorer.WithExtensionVersions(version.ToString()));
        }

        var app = this.inner.Build();

        // GET /health: outside the correlation middleware (which is path-scoped to /resource/ and
        // /longRunningOperation/), and there is no authz middleware, so no .AllowAnonymous() is needed.
        app.MapHealthChecks("/health");

        return app;
    }
}
