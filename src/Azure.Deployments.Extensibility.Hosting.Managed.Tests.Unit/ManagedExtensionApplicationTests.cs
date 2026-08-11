// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Behaviors;
using Azure.Deployments.Extensibility.AspNetCore.Extensions;
using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using Azure.Deployments.Extensibility.Hosting.Managed.Extensions;
using Azure.Deployments.Extensibility.Hosting.Managed.Metadata;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace Azure.Deployments.Extensibility.Hosting.Managed.Tests.Unit;

public class ManagedExtensionApplicationTests
{
    private static readonly BicepExtensionDescriptor Descriptor = new("Widget", "1.0.0");

    [Fact]
    public void UseBicepExtension_WithoutServiceRegistration_Throws()
    {
        var builder = WebApplication.CreateBuilder();
        using var app = builder.Build();

        var action = () => app.UseBicepExtension();

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void UseBicepExtension_CalledTwice_Throws()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddBicepExtension(extension => extension.AddHandler<GetHandler>(), Descriptor);
        using var app = builder.Build();
        app.UseBicepExtension();

        var action = () => app.UseBicepExtension();

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("UseBicepExtension can only be called once.");
    }

    [Fact]
    public async Task Application_WithoutUseBicepExtension_FailsStartup()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddBicepExtension(extension => extension.AddHandler<GetHandler>(), Descriptor);
        await using var app = builder.Build();

        var action = () => app.StartAsync();

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Call UseBicepExtension before starting the application.");
    }

    [Fact]
    public void UseBicepExtension_ValidRegistration_MapsBareContractRoutes()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddBicepExtension(extension => extension.AddHandler<GetHandler>(), Descriptor);
        using var app = builder.Build();

        app.UseBicepExtension();

        var routes = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(dataSource => dataSource.Endpoints)
            .OfType<RouteEndpoint>()
            .Select(endpoint => endpoint.RoutePattern.RawText)
            .ToArray();
        routes.Should().Contain([
            "/{extensionVersion}/resource/preview",
            "/{extensionVersion}/resource/createOrUpdate",
            "/{extensionVersion}/resource/get",
            "/{extensionVersion}/resource/delete",
            "/{extensionVersion}/longRunningOperation/get",
        ]);
    }

    [Fact]
    public async Task Ping_RequestMethod_IsGetOnly()
    {
        await using var app = await StartApplicationAsync(extension => extension.AddHandler<GetHandler>());
        var client = app.GetTestClient();

        var getResponse = await client.GetAsync("/ping");
        var postResponse = await client.PostAsync("/ping", content: null);

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        postResponse.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    }

    [Fact]
    public async Task MapDevelopmentApiExplorer_ConfiguredVersion_UsesIdentityVersion()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Environment.EnvironmentName = Environments.Development;
        builder.WebHost.UseTestServer();
        builder.Services.AddBicepExtension(extension => extension.AddHandler<GetHandler>(), Descriptor);
        await using var app = builder.Build();
        app.UseBicepExtension();
        app.MapDevelopmentApiExplorer(explorer => explorer
            .WithTitle("Widget Extension API")
            .WithExtensionVersions("9.9.9"));
        await app.StartAsync();
        var client = app.GetTestClient();

        var documentResponse = await client.GetAsync("/openapi/v2.json");
        var explorerResponse = await client.GetAsync("/scalar/v2");

        documentResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        explorerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var document = JsonNode.Parse(await documentResponse.Content.ReadAsStringAsync());
        document!["info"]!["title"]!.GetValue<string>().Should().Be("Widget Extension API");
        document["components"]!["parameters"]!["RequestParams.extensionVersion"]!["example"]!
            .GetValue<string>().Should().Be(Descriptor.Version);
    }

    [Fact]
    public async Task Dispatch_RouteVersion_RequiresExactIdentityVersion()
    {
        await using var app = await StartApplicationAsync(extension => extension
            .ForResourceType("Widget", resourceType => resourceType.AddHandler<GetHandler>()));
        var client = app.GetTestClient();
        AddContractHeaders(client);
        var request = new ResourceReference
        {
            Type = "Widget",
            Identifiers = new JsonObject { ["name"] = "example" },
        };

        var exactResponse = await client.PostAsJsonAsync("/1.0.0/resource/get", request);
        var mismatchResponse = await client.PostAsJsonAsync("/1.0/resource/get", request);

        exactResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        mismatchResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await mismatchResponse.Content.ReadFromJsonAsync<ErrorResponse>();
        error!.Error.Code.Should().Be("UnsupportedExtensionVersion");
    }

    [Fact]
    public async Task Dispatch_VersionMismatch_RunsGlobalBehavior()
    {
        var recorder = new BehaviorRecorder();
        await using var app = await StartApplicationAsync(
            extension => extension
                .AddGlobalHandlerBehavior(_ => new RecordingBehavior(recorder))
                .AddHandler<GetHandler>());
        var client = app.GetTestClient();
            AddContractHeaders(client);

        await client.PostAsJsonAsync(
            "/2.0.0/resource/get",
            new ResourceReference
            {
                Type = "Widget",
                Identifiers = new JsonObject { ["name"] = "example" },
            });

        recorder.InvocationCount.Should().Be(1);
    }

    [Fact]
    public async Task HandlerFactory_AcrossRequests_UsesAndDisposesRequestScopes()
    {
        var tracker = new FactoryTracker();
        await using var app = await StartApplicationAsync(
            extension => extension.AddHandler(_ => new FactoryGetHandler(tracker)));
        var client = app.GetTestClient();
        AddContractHeaders(client);
        var request = new ResourceReference
        {
            Type = "Widget",
            Identifiers = new JsonObject { ["name"] = "example" },
        };

        await client.PostAsJsonAsync("/1.0.0/resource/get", request);
        await client.PostAsJsonAsync("/1.0.0/resource/get", request);

        tracker.Created.Should().Be(2);
        tracker.Disposed.Should().Be(2);
    }

    [Fact]
    public async Task Application_WithUserMiddlewareServiceAndEndpoint_PreservesAspNetCoreComposition()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(new UserService("available"));
        builder.Services.AddBicepExtension(extension => extension.AddHandler<GetHandler>(), Descriptor);
        await using var app = builder.Build();
        app.Use(async (context, next) =>
        {
            context.Response.Headers["x-user-middleware"] = "applied";
            await next(context);
        });
        app.UseBicepExtension();
        app.MapGet("/about", (UserService service) => service.Value);
        await app.StartAsync();

        var response = await app.GetTestClient().GetAsync("/about");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.GetValues("x-user-middleware").Should().ContainSingle("applied");
        (await response.Content.ReadAsStringAsync()).Should().Be("available");
    }

    private static async Task<WebApplication> StartApplicationAsync(
        Action<IBicepManagedExtensionBuilder> configure)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddBicepExtension(configure, Descriptor);
        var app = builder.Build();
        app.UseBicepExtension();
        await app.StartAsync();

        return app;
    }

    private static void AddContractHeaders(HttpClient client)
    {
        client.DefaultRequestHeaders.Add(
            "x-ms-client-request-id",
            "00000000-0000-0000-0000-000000000001");
        client.DefaultRequestHeaders.Add(
            "x-ms-correlation-request-id",
            "00000000-0000-0000-0000-000000000002");
    }

    private sealed class GetHandler : IResourceGetHandler
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

    private sealed class RecordingBehavior : IResourceGetBehavior
    {
        private readonly BehaviorRecorder recorder;

        public RecordingBehavior(BehaviorRecorder recorder)
        {
            this.recorder = recorder;
        }

        public Task<OneOf<Resource?, ErrorResponse>> HandleAsync(
            ResourceReference request,
            ResourceGetHandlerDelegate next,
            CancellationToken cancellationToken)
        {
            this.recorder.InvocationCount++;
            return next(request);
        }
    }

    private sealed class FactoryGetHandler : IResourceGetHandler, IDisposable
    {
        private readonly FactoryTracker tracker;

        public FactoryGetHandler(FactoryTracker tracker)
        {
            this.tracker = tracker;
            this.tracker.Created++;
        }

        public Task<OneOf<Resource?, ErrorResponse>> HandleAsync(
            ResourceReference request,
            CancellationToken cancellationToken) =>
            Task.FromResult<OneOf<Resource?, ErrorResponse>>((Resource?)null);

        public void Dispose() => this.tracker.Disposed++;
    }

    private sealed class BehaviorRecorder
    {
        public int InvocationCount { get; set; }
    }

    private sealed class FactoryTracker
    {
        public int Created { get; set; }

        public int Disposed { get; set; }
    }

    private sealed record UserService(string Value);
}
