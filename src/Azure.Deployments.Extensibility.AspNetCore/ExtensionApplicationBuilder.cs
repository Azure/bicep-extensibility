// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using Azure.Deployments.Extensibility.Core.V2.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Azure.Deployments.Extensibility.AspNetCore;

/// <summary>
/// Builder for registering extensibility handlers and validators with the dependency injection container.
/// </summary>
public class ExtensionApplicationBuilder
{
    public ExtensionApplicationBuilder(IServiceCollection services)
    {
        this.Services = services;
    }

    /// <summary>
    /// Gets the service collection used for registering handlers and validators.
    /// </summary>
    public IServiceCollection Services { get; }

    /// <summary>
    /// Registers a scoped resource preview handler.
    /// </summary>
    public ExtensionApplicationBuilder AddResourcePreviewHandler<THandler>()
        where THandler : class, IResourcePreviewHandler
    {
        this.Services.AddScoped<IResourcePreviewHandler, THandler>();

        return this;
    }

    /// <summary>
    /// Registers a scoped resource create-or-update handler.
    /// </summary>
    public ExtensionApplicationBuilder AddResourceCreateOrUpdateHandler<THandler>()
        where THandler : class, IResourceCreateOrUpdateHandler
    {
        this.Services.AddScoped<IResourceCreateOrUpdateHandler, THandler>();

        return this;
    }

    /// <summary>
    /// Registers a scoped resource get handler.
    /// </summary>
    public ExtensionApplicationBuilder AddResourceGetHandler<THandler>()
        where THandler : class, IResourceGetHandler
    {
        this.Services.AddScoped<IResourceGetHandler, THandler>();

        return this;
    }

    /// <summary>
    /// Registers a scoped resource delete handler.
    /// </summary>
    public ExtensionApplicationBuilder AddResourceDeleteHandler<THandler>()
        where THandler : class, IResourceDeleteHandler
    {
        this.Services.AddScoped<IResourceDeleteHandler, THandler>();

        return this;
    }

    /// <summary>
    /// Registers a scoped long-running operation get handler.
    /// </summary>
    public ExtensionApplicationBuilder AddLongRunningOperationGetHandler<THandler>()
        where THandler : class, ILongRunningOperationGetHandler
    {
        this.Services.AddScoped<ILongRunningOperationGetHandler, THandler>();

        return this;
    }

    /// <summary>
    /// Registers a singleton validator for <see cref="ResourceSpecification"/>.
    /// </summary>
    public ExtensionApplicationBuilder AddResourceSpecificationValidator<TValidator>()
        where TValidator : class, IModelValidator<ResourceSpecification>
    {
        this.Services.AddSingleton<IModelValidator<ResourceSpecification>, TValidator>();

        return this;
    }

    /// <summary>
    /// Registers a singleton validator for <see cref="ResourcePreviewSpecification"/>.
    /// </summary>
    public ExtensionApplicationBuilder AddResourcePreviewSpecificationValidator<TValidator>()
        where TValidator : class, IModelValidator<ResourcePreviewSpecification>
    {
        this.Services.AddSingleton<IModelValidator<ResourcePreviewSpecification>, TValidator>();

        return this;
    }

    /// <summary>
    /// Registers a singleton validator for <see cref="ResourceReference"/>.
    /// </summary>
    public ExtensionApplicationBuilder AddResourceReferenceValidator<TValidator>()
        where TValidator : class, IModelValidator<ResourceReference>
    {
        this.Services.AddSingleton<IModelValidator<ResourceReference>, TValidator>();

        return this;
    }
}
