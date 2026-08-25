// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace Azure.Deployments.Extensibility.Hosting.Tests.Unit;

public class ManagedBicepExtensionTests
{
    [Fact]
    public async Task AddBicepExtension_MapsEndpoints()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.AddBicepExtension("1.0.0", extension => extension.AddHandler<EndpointGetHandler>());

        await using var app = builder.Build();
        app.UseBicepExtension();
        await app.StartAsync();

        var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add("x-ms-client-request-id", "00000000-0000-0000-0000-000000000001");
        client.DefaultRequestHeaders.Add("x-ms-correlation-request-id", "00000000-0000-0000-0000-000000000002");

        var response = await client.PostAsJsonAsync(
            "/1.0.0/resource/get",
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
    public async Task EnableDevelopmentScalarApiExplorer_MapsOpenApiEndpoint()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = Environments.Development;
        builder.AddBicepExtension("1.0.0", extension => extension.AddHandler<EndpointGetHandler>());

        await using var app = builder.Build();
        app.UseBicepExtension();
        app.EnableDevelopmentScalarApiExplorer(explorer => explorer.WithTitle("My Extension API"));
        await app.StartAsync();

        var client = app.GetTestClient();
        var response = await client.GetAsync("/openapi/v2.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("My Extension API");
    }

    [Fact]
    public void UseBicepExtension_Twice_Throws()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddBicepExtension("1.0.0", extension => extension.AddHandler<EndpointGetHandler>());

        using var app = builder.Build();
        app.UseBicepExtension();

        var action = () => app.UseBicepExtension();

        action.Should().Throw<InvalidOperationException>().WithMessage("*already been configured*");
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
}
