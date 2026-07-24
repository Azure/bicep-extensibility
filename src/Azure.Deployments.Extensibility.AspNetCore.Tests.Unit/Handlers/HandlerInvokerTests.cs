// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Behaviors;
using Azure.Deployments.Extensibility.AspNetCore.Handlers;
using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Exceptions;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Nodes;
using Xunit;

namespace Azure.Deployments.Extensibility.AspNetCore.Tests.Unit.Handlers;

public class HandlerInvokerTests
{
    [Fact]
    public async Task Invoke_WithResolvedRegistration_OrdersBehaviors()
    {
        var events = new List<string>();
        var services = new ServiceCollection();
        services.AddBicepExtensionGlobalHandlerBehavior(_ => new RecordingGetBehavior(events, "global"));
        var registration = BicepExtensionRegistration.Create(services, extension => extension
            .AddHandlerBehavior(_ => new RecordingGetBehavior(events, "registration"))
            .ForResourceType("Widget", resourceType => resourceType
                .AddHandler(_ => new RecordingGetHandler(events))
                .AddHandlerBehavior(_ => new RecordingGetBehavior(events, "resource"))));
        services.AddSingleton<IBicepExtensionResolver>(new StubResolver(registration));
        services.AddBicepExtensionHandlerRuntime();
        using var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scope = serviceProvider.CreateScope();
        var sut = scope.ServiceProvider.GetRequiredService<HandlerInvoker>();

        var result = await sut.InvokeResourceGetAsync("1.0.0", CreateReference(), CancellationToken.None);

        result.IsT0.Should().BeTrue();
        events.Should().Equal(
            "global-before",
            "registration-before",
            "resource-before",
            "handler",
            "resource-after",
            "registration-after",
            "global-after");
    }

    [Fact]
    public async Task Invoke_WithUnsupportedVersion_RunsGlobalBehaviors()
    {
        var events = new List<string>();
        var services = new ServiceCollection();
        services.AddBicepExtensionGlobalHandlerBehavior(_ => new RecordingGetBehavior(events, "global"));
        services.AddSingleton<IBicepExtensionResolver>(new StubResolver(registration: null));
        services.AddBicepExtensionHandlerRuntime();
        using var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scope = serviceProvider.CreateScope();
        var sut = scope.ServiceProvider.GetRequiredService<HandlerInvoker>();

        var result = await sut.InvokeResourceGetAsync("invalid", CreateReference(), CancellationToken.None);

        result.IsT1.Should().BeTrue();
        result.AsT1!.Error.Code.Should().Be("UnsupportedExtensionVersion");
        events.Should().Equal("global-before", "global-after");
    }

    [Fact]
    public async Task Invoke_WhenGlobalThrowsErrorResponse_ReturnsError()
    {
        var services = new ServiceCollection();
        services.AddBicepExtensionGlobalHandlerBehavior<ThrowingGetBehavior>();
        services.AddSingleton<IBicepExtensionResolver>(new StubResolver(registration: null));
        services.AddBicepExtensionHandlerRuntime();
        using var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scope = serviceProvider.CreateScope();
        var sut = scope.ServiceProvider.GetRequiredService<HandlerInvoker>();

        var result = await sut.InvokeResourceGetAsync("1.0.0", CreateReference(), CancellationToken.None);

        result.IsT1.Should().BeTrue();
        result.AsT1!.Error.Code.Should().Be("ExpectedFailure");
    }

    [Fact]
    public async Task Invoke_WithoutLroHandler_ReturnsUnsupportedOperation()
    {
        var services = new ServiceCollection();
        var registration = BicepExtensionRegistration.Create(services, extension => extension.AddHandler<RecordingGetHandler>());
        services.AddSingleton<IBicepExtensionResolver>(new StubResolver(registration));
        services.AddBicepExtensionHandlerRuntime();
        using var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scope = serviceProvider.CreateScope();
        var sut = scope.ServiceProvider.GetRequiredService<HandlerInvoker>();

        var result = await sut.InvokeLongRunningOperationGetAsync("1.0.0", [], CancellationToken.None);

        result.IsT1.Should().BeTrue();
        result.AsT1!.Error.Code.Should().Be("UnsupportedOperation");
    }

    private static ResourceReference CreateReference() => new()
    {
        Type = "Widget",
        Identifiers = new JsonObject { ["name"] = "example" },
    };

    private sealed class StubResolver : IBicepExtensionResolver
    {
        private readonly BicepExtensionRegistration? registration;

        public StubResolver(BicepExtensionRegistration? registration)
        {
            this.registration = registration;
        }

        public BicepExtensionRegistration? Resolve(string extensionVersion) => this.registration;
    }

    private sealed class RecordingGetHandler : IResourceGetHandler
    {
        private readonly IList<string>? events;

        public RecordingGetHandler()
        {
        }

        public RecordingGetHandler(IList<string> events)
        {
            this.events = events;
        }

        public Task<OneOf<Resource?, ErrorResponse>> HandleAsync(
            ResourceReference request,
            CancellationToken cancellationToken)
        {
            this.events?.Add("handler");

            return Task.FromResult<OneOf<Resource?, ErrorResponse>>(new Resource
            {
                Type = request.Type,
                Identifiers = request.Identifiers,
                Properties = new JsonObject { ["name"] = "example" },
            });
        }
    }

    private sealed class RecordingGetBehavior : IResourceGetBehavior
    {
        private readonly IList<string> events;
        private readonly string name;

        public RecordingGetBehavior(IList<string> events, string name)
        {
            this.events = events;
            this.name = name;
        }

        public async Task<OneOf<Resource?, ErrorResponse>> HandleAsync(
            ResourceReference request,
            ResourceGetHandlerDelegate next,
            CancellationToken cancellationToken)
        {
            this.events.Add($"{this.name}-before");
            var result = await next(request);
            this.events.Add($"{this.name}-after");

            return result;
        }
    }

    private sealed class ThrowingGetBehavior : IResourceGetBehavior
    {
        public Task<OneOf<Resource?, ErrorResponse>> HandleAsync(
            ResourceReference request,
            ResourceGetHandlerDelegate next,
            CancellationToken cancellationToken) =>
            throw new ErrorResponseException("ExpectedFailure", "Expected failure.");
    }
}
