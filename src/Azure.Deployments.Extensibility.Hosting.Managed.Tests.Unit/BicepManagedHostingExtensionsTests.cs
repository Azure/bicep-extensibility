// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore;
using Azure.Deployments.Extensibility.AspNetCore.Extensions;
using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using Azure.Deployments.Extensibility.Hosting.Managed;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.Json.Nodes;
using Xunit;

namespace Azure.Deployments.Extensibility.Hosting.Managed.Tests.Unit;

public class BicepManagedHostingExtensionsTests
{
    private static readonly BicepExtensionIdentity Identity = new("Test.Extension", "1.0.0");

    [Fact]
    public void Resolver_MatchesOnlyExactVersionOrdinally()
    {
        var services = new ServiceCollection();
        var registration = BicepExtensionRegistration.Create(
            services,
            extension => extension.AddHandler<StubGetHandler>());
        var resolver = new ManagedBicepExtensionResolver(Identity, registration);

        resolver.Resolve("1.0.0").Should().BeSameAs(registration);
        resolver.Resolve("1.0.0-preview").Should().BeNull();
        resolver.Resolve("01.0.0").Should().BeNull();
        resolver.Resolve("invalid").Should().BeNull();
    }

    [Fact]
    public void AddBicepExtension_WhenCalledTwice_Throws()
    {
        var builder = WebApplication.CreateBuilder();
        BicepManagedHostingExtensions.AddBicepExtension(
            builder,
            extension => extension.AddHandler<StubGetHandler>(),
            Identity);

        var action = () => BicepManagedHostingExtensions.AddBicepExtension(
            builder,
            extension => extension.AddHandler<StubGetHandler>(),
            Identity);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void UseBicepExtension_WithoutRegistration_Throws()
    {
        var builder = WebApplication.CreateBuilder();
        using var app = builder.Build();

        var action = () => app.UseBicepExtension();

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void IdentityReader_WithoutMetadata_ThrowsClearly()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("MissingManagedExtensionIdentity"),
            AssemblyBuilderAccess.Run);

        var action = () => ManagedExtensionIdentityReader.Read(assembly);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*BicepExtensionName*");
    }

    [Fact]
    public async Task UseBicepExtension_MapsHealthAndPreservesHostEndpointsAndMiddleware()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Development",
        });
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(new HostService("available"));
        BicepManagedHostingExtensions.AddBicepExtension(
            builder,
            extension => extension.AddHandler<StubGetHandler>(),
            Identity);
        await using var app = builder.Build();
        app.Use(async (context, next) =>
        {
            context.Response.Headers["x-host-middleware"] = "true";
            await next();
        });
        app.UseBicepExtension();
        app.MapBicepExtensionApiExplorer(
            title: "Managed Test Extension API",
            extensionVersions: [Identity.Version]);
        app.MapGet("/host", (HostService service) => service.Value);
        await app.StartAsync();
        var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add("x-ms-client-request-id", "00000000-0000-0000-0000-000000000001");
        client.DefaultRequestHeaders.Add("x-ms-correlation-request-id", "00000000-0000-0000-0000-000000000002");

        var ping = await client.GetAsync("/ping");
        var host = await client.GetAsync("/host");
        var openApi = await client.GetAsync("/openapi/v2.json");
        var request = new ResourceReference
        {
            Type = "Widget",
            Identifiers = new JsonObject { ["name"] = "example" },
        };
        var supported = await client.PostAsJsonAsync("/1.0.0/resource/get", request);
        var unsupported = await client.PostAsJsonAsync("/1.0.0-preview/resource/get", request);

        ping.StatusCode.Should().Be(HttpStatusCode.OK);
        ping.Headers.GetValues("x-host-middleware").Should().ContainSingle("true");
        (await host.Content.ReadAsStringAsync()).Should().Be("available");
        openApi.StatusCode.Should().Be(HttpStatusCode.OK);
        supported.StatusCode.Should().Be(HttpStatusCode.OK);
        unsupported.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public void UseBicepExtension_WhenCalledTwice_Throws()
    {
        var builder = WebApplication.CreateBuilder();
        BicepManagedHostingExtensions.AddBicepExtension(
            builder,
            extension => extension.AddHandler<StubGetHandler>(),
            Identity);
        using var app = builder.Build();
        app.UseBicepExtension();

        var action = () => app.UseBicepExtension();

        action.Should().Throw<InvalidOperationException>();
    }

    private sealed record HostService(string Value);

    private sealed class StubGetHandler : IResourceGetHandler
    {
        public Task<OneOf<Resource?, ErrorResponse>> HandleAsync(
            ResourceReference request,
            CancellationToken cancellationToken) =>
            Task.FromResult<OneOf<Resource?, ErrorResponse>>(
                new Resource
                {
                    Type = request.Type,
                    Identifiers = request.Identifiers,
                    Properties = new JsonObject(),
                });
    }
}
