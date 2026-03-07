// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Semver;
using System.Reflection;

namespace Azure.Deployments.Extensibility.AspNetCore.Handlers;

/// <summary>
/// Abstract base class for handlers that require access to the current <see cref="HttpContext"/>.
/// Provides a lazily-resolved <see cref="ILogger"/> and automatic version resolution via <see cref="SupportedExtensionVersionsAttribute"/>.
/// </summary>
public abstract class HttpContextAwareHandler : IHandler
{
    private readonly Lazy<ILogger> logger;

    protected HttpContextAwareHandler(IHttpContextAccessor httpContextAccessor)
    {
        this.HttpContext = httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No active HttpContext.");
        this.logger = new(() => this.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(this.GetType()));
    }

    public virtual SemVersionRange SupportedExtensionVersions =>
        this.GetType().GetCustomAttribute<SupportedExtensionVersionsAttribute>()?.Range
        ?? throw new InvalidOperationException($"The handler '{this.GetType().Name}' must either override {nameof(this.SupportedExtensionVersions)} or apply [{nameof(SupportedExtensionVersionsAttribute)}].");

    protected HttpContext HttpContext { get; }

    /// <summary>
    /// Gets an <see cref="ILogger"/> scoped to the concrete handler type.
    /// Lazily resolved on first access.
    /// </summary>
    protected ILogger Logger => this.logger.Value;
}
