// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Extensions;
using Azure.Deployments.Extensibility.AspNetCore.Exceptions;
using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace Azure.Deployments.Extensibility.AspNetCore.Tests.Unit.Handlers;

public class BicepExtensionEndpointTests
{
    [Fact]
    public void MapEndpoints_WithoutResolver_Throws()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddBicepExtensionHandlerRuntime();
        using var app = builder.Build();

        var action = () => app.MapBicepExtensionEndpoints();

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MapEndpoints_WithMultipleResolvers_Throws()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddBicepExtensionHandlerRuntime();
        var registration = BicepExtensionRegistration.Create(builder.Services, extension => extension
            .AddHandler<EndpointGetHandler>());
        builder.Services.AddSingleton<IBicepExtensionResolver>(new StubResolver(registration));
        builder.Services.AddSingleton<IBicepExtensionResolver>(new StubResolver(registration));
        using var app = builder.Build();

        var action = () => app.MapBicepExtensionEndpoints();

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task MapEndpoints_UnderRouteGroup_DispatchesRequest()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddBicepExtensionServices();
        var registration = BicepExtensionRegistration.Create(builder.Services, extension => extension
            .ForResourceType("Widget", resourceType => resourceType.AddHandler<EndpointGetHandler>()));
        builder.Services.AddSingleton<IBicepExtensionResolver>(new StubResolver(registration));
        await using var app = builder.Build();
        app.UseBicepExtensionMiddlewares();
        app.MapGroup("/extension/apis").MapBicepExtensionEndpoints();
        await app.StartAsync();
        var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add("x-ms-client-request-id", "00000000-0000-0000-0000-000000000001");
        client.DefaultRequestHeaders.Add("x-ms-correlation-request-id", "00000000-0000-0000-0000-000000000002");

        var response = await client.PostAsJsonAsync(
            "/extension/apis/1.0.0/resource/get",
            new ResourceReference
            {
                Type = "Widget",
                Identifiers = new JsonObject { ["name"] = "example" },
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var resource = await response.Content.ReadFromJsonAsync<Resource>();
        resource.Should().NotBeNull();
        resource!.Type.Should().Be("Widget");
    }

    [Fact]
    public async Task MapEndpoints_WithoutLroHandler_ReturnsProtocolError()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddBicepExtensionServices();
        var registration = BicepExtensionRegistration.Create(builder.Services, extension => extension
            .AddHandler<EndpointGetHandler>());
        builder.Services.AddSingleton<IBicepExtensionResolver>(new StubResolver(registration));
        await using var app = builder.Build();
        app.MapBicepExtensionEndpoints();
        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.PostAsJsonAsync(
            "/1.0.0/longRunningOperation/get",
            new JsonObject { ["operationId"] = "example" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error.Should().NotBeNull();
        error!.Error.Code.Should().Be("UnsupportedOperation");
    }

    [Fact]
    public async Task MapEndpoints_WithHttpErrorResponse_PreservesStatusCode()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddBicepExtensionServices();
        var registration = BicepExtensionRegistration.Create(builder.Services, extension => extension
            .AddHandler<ThrowingGetHandler>());
        builder.Services.AddSingleton<IBicepExtensionResolver>(new StubResolver(registration));
        await using var app = builder.Build();
        app.MapBicepExtensionEndpoints();
        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.PostAsJsonAsync(
            "/1.0.0/resource/get",
            new ResourceReference
            {
                Type = "Widget",
                Identifiers = new JsonObject { ["name"] = "example" },
            });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error.Should().NotBeNull();
        error!.Error.Code.Should().Be("Conflict");
    }

    private sealed class StubResolver : IBicepExtensionResolver
    {
        private readonly BicepExtensionRegistration registration;

        public StubResolver(BicepExtensionRegistration registration)
        {
            this.registration = registration;
        }

        public BicepExtensionRegistration? Resolve(string extensionVersion) => this.registration;
    }

    private sealed class EndpointGetHandler : IResourceGetHandler
    {
        public Task<OneOf<Resource?, ErrorResponse>> HandleAsync(
            ResourceReference request,
            CancellationToken cancellationToken) =>
            Task.FromResult<OneOf<Resource?, ErrorResponse>>(new Resource
            {
                Type = request.Type,
                Identifiers = request.Identifiers,
                Properties = new JsonObject { ["name"] = "example" },
            });
    }

    private sealed class ThrowingGetHandler : IResourceGetHandler
    {
        public Task<OneOf<Resource?, ErrorResponse>> HandleAsync(
            ResourceReference request,
            CancellationToken cancellationToken) =>
            throw new HttpErrorResponseException((int)HttpStatusCode.Conflict, "Conflict", "Expected conflict.");
    }
}
