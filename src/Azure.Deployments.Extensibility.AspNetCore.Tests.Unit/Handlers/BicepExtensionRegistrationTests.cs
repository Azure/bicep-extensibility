// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Handlers;
using Azure.Deployments.Extensibility.AspNetCore.Behaviors;
using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Nodes;
using Xunit;

namespace Azure.Deployments.Extensibility.AspNetCore.Tests.Unit.Handlers;

public class BicepExtensionRegistrationTests
{
    [Fact]
    public void Create_WithNoHandlers_Throws()
    {
        var services = new ServiceCollection();

        var action = () => BicepExtensionRegistration.Create(services, _ => { });

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Create_WithDuplicateOperation_Throws()
    {
        var services = new ServiceCollection();

        var action = () => BicepExtensionRegistration.Create(services, extension => extension
            .AddHandler<StubGetHandler>()
            .AddHandler<SecondGetHandler>());

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Create_WithResourceScopedLroHandler_Throws()
    {
        var services = new ServiceCollection();

        var action = () => BicepExtensionRegistration.Create(services, extension => extension
            .ForResourceType("Widget", resourceType => resourceType.AddHandler<StubLroHandler>()));

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Create_WithUnsupportedBehaviorShape_Throws()
    {
        var services = new ServiceCollection();

        var action = () => BicepExtensionRegistration.Create(services, extension => extension
            .AddHandler<StubGetHandler>()
            .AddHandlerBehavior<UnsupportedBehavior>());

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ResolveHandler_WithCaseInsensitiveResourceType_ReturnsHandler()
    {
        var services = new ServiceCollection();
        var registration = BicepExtensionRegistration.Create(services, extension => extension
            .ForResourceType("Widget", resourceType => resourceType.AddHandler<StubGetHandler>()));
        using var serviceProvider = services.BuildServiceProvider();

        var handler = registration.ResolveHandler<IResourceGetHandler>("widget", serviceProvider);

        handler.Should().BeOfType<StubGetHandler>();
    }

    [Fact]
    public void ResolveHandler_WithSameTypeFactories_UsesIndependentRegistrations()
    {
        var services = new ServiceCollection();
        var registration = BicepExtensionRegistration.Create(services, extension => extension
            .ForResourceType("First", resourceType => resourceType.AddHandler(_ => new ConfiguredGetHandler("first")))
            .ForResourceType("Second", resourceType => resourceType.AddHandler(_ => new ConfiguredGetHandler("second"))));
        using var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scope = serviceProvider.CreateScope();

        var first = registration.ResolveHandler<IResourceGetHandler>("First", scope.ServiceProvider);
        var second = registration.ResolveHandler<IResourceGetHandler>("Second", scope.ServiceProvider);

        first.Should().BeOfType<ConfiguredGetHandler>().Which.Name.Should().Be("first");
        second.Should().BeOfType<ConfiguredGetHandler>().Which.Name.Should().Be("second");
    }

    [Fact]
    public void FactoryCreatedHandler_WhenScopeEnds_IsDisposed()
    {
        var services = new ServiceCollection();
        DisposableGetHandler? createdHandler = null;
        var registration = BicepExtensionRegistration.Create(services, extension => extension
            .AddHandler(_ => createdHandler = new DisposableGetHandler()));
        using var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        using (var scope = serviceProvider.CreateScope())
        {
            registration.ResolveHandler<IResourceGetHandler>(resourceType: null, scope.ServiceProvider)
                .Should().BeSameAs(createdHandler);
        }

        createdHandler.Should().NotBeNull();
        createdHandler!.Disposed.Should().BeTrue();
    }

    private class StubGetHandler : IResourceGetHandler
    {
        public virtual Task<OneOf<Resource?, ErrorResponse>> HandleAsync(
            ResourceReference request,
            CancellationToken cancellationToken) =>
            Task.FromResult<OneOf<Resource?, ErrorResponse>>((Resource?)null);
    }

    private sealed class SecondGetHandler : StubGetHandler
    {
    }

    private sealed class ConfiguredGetHandler : StubGetHandler
    {
        public ConfiguredGetHandler(string name)
        {
            this.Name = name;
        }

        public string Name { get; }
    }

    private sealed class DisposableGetHandler : StubGetHandler, IDisposable
    {
        public bool Disposed { get; private set; }

        public void Dispose()
        {
            this.Disposed = true;
        }
    }

    private sealed class StubLroHandler : ILongRunningOperationGetHandler
    {
        public Task<OneOf<LongRunningOperation, ErrorResponse>> HandleAsync(
            JsonObject request,
            CancellationToken cancellationToken) =>
            Task.FromResult<OneOf<LongRunningOperation, ErrorResponse>>(
                new ErrorResponse(new Error("NotImplemented", "Not implemented.")));
    }

    private sealed class UnsupportedBehavior : IHandlerBehavior<string, string>
    {
        public Task<string> HandleAsync(
            string request,
            HandlerDelegate<string, string> next,
            CancellationToken cancellationToken) => next(request);
    }
}
