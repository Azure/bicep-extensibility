// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net;
using System.Text.Json.Nodes;
using Xunit;

namespace Azure.Deployments.Extensibility.AspNetCore.Tests.Unit;

public class ScalarApiExplorerTests
{
    [Fact]
    public async Task MapDevelopment_MultipleVersions_MapsConfiguredExplorer()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Environment.EnvironmentName = Environments.Development;
        builder.WebHost.UseTestServer();
        builder.Services.AddBicepExtensionJsonOptions();
        await using var app = builder.Build();
        BicepExtensionApiExplorer.MapDevelopment(app, explorer => explorer
            .WithTitle("Widget Extension API")
            .WithExtensionVersions("1.0.0", "2.0.0"));
        await app.StartAsync();
        var client = app.GetTestClient();

        var documentResponse = await client.GetAsync("/openapi/v2.json");
        var explorerResponse = await client.GetAsync("/scalar/v2");

        documentResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        explorerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var document = JsonNode.Parse(await documentResponse.Content.ReadAsStringAsync());
        document!["info"]!["title"]!.GetValue<string>().Should().Be("Widget Extension API");
        var versionExamples = document["components"]!["parameters"]!["RequestParams.extensionVersion"]!["examples"]!;
        versionExamples["1.0.0"]!["value"]!.GetValue<string>().Should().Be("1.0.0");
        versionExamples["2.0.0"]!["value"]!.GetValue<string>().Should().Be("2.0.0");
    }

    [Fact]
    public async Task MapDevelopment_Production_DoesNotMapExplorer()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Environment.EnvironmentName = Environments.Production;
        builder.WebHost.UseTestServer();
        await using var app = builder.Build();
        BicepExtensionApiExplorer.MapDevelopment(app);
        await app.StartAsync();
        var client = app.GetTestClient();

        var documentResponse = await client.GetAsync("/openapi/v2.json");
        var explorerResponse = await client.GetAsync("/scalar/v2");

        documentResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        explorerResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
