// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using Xunit;

namespace Azure.Deployments.Extensibility.AspNetCore.Tests.Unit.Extensions;

public class ScalarExtensionsTests
{
    [Fact]
    public async Task MapBicepExtensionApiExplorer_InDevelopment_MapsConfiguredDocument()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Development",
        });
        builder.WebHost.UseTestServer();
        builder.Services.AddBicepExtensionJsonOptions();
        await using var app = builder.Build();
        app.MapBicepExtensionApiExplorer(
            title: "Test Extension API",
            extensionVersions: ["1.0.0"]);
        await app.StartAsync();

        var response = await app.GetTestClient().GetAsync("/openapi/v2.json");
        var document = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        document.Should().Contain("\"title\": \"Test Extension API\"");
        document.Should().Contain("\"1.0.0\"");
    }

    [Fact]
    public async Task MapBicepExtensionApiExplorer_OutsideDevelopment_DoesNotMapRoutes()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Production",
        });
        builder.WebHost.UseTestServer();
        await using var app = builder.Build();
        app.MapBicepExtensionApiExplorer();
        await app.StartAsync();

        var response = await app.GetTestClient().GetAsync("/openapi/v2.json");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
