// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Builders;
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

public class ScalarExtensionsTests
{
    [Fact]
    public async Task MapBicepExtensionApiExplorer_InDevelopment_ExposesOpenApiJsonAndScalarUi()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Environment.EnvironmentName = Environments.Development;
        builder.WebHost.UseTestServer();
        builder.Services.AddBicepExtensionServices();

        await using var app = builder.Build();
        app.MapBicepExtensionApiExplorer(explorer => explorer
            .WithTitle("Custom Extension API")
            .WithExtensionVersions("1.0.0", "2.0.0")
            .ConfigureExamples(examples =>
            {
                examples.ForPreview(
                    request: new { type = "TestResource", properties = new { name = "preview" } },
                    response: new { type = "TestResource", identifiers = new { name = "preview" }, properties = new { name = "preview" } });
                examples.ForCreateOrUpdate(
                    request: new { type = "TestResource", properties = new { name = "created" } },
                    response: new { type = "TestResource", identifiers = new { name = "created" }, properties = new { name = "created" } });
                examples.ForGet(
                    request: new { type = "TestResource", identifiers = new { name = "my-resource" } },
                    response: new { type = "TestResource", identifiers = new { name = "my-resource" }, properties = new { name = "my-resource" } });
                examples.ForDelete(
                    request: new { type = "TestResource", identifiers = new { name = "my-resource" } });
                examples.ForLongRunningOperationGet(
                    request: new { id = "op-123" },
                    response: new { id = "op-123", status = "Succeeded" });
            }));

        await app.StartAsync();
        var client = app.GetTestClient();

        var openApiResponse = await client.GetAsync("/openapi/v2.json");
        openApiResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var openApiContent = await openApiResponse.Content.ReadAsStringAsync();
        var openApiDoc = JsonNode.Parse(openApiContent);
        openApiDoc.Should().NotBeNull();
        openApiDoc!["info"]?["title"]?.GetValue<string>().Should().Be("Custom Extension API");

        // Verify parameter examples
        var extensionVersionParam = openApiDoc["components"]?["parameters"]?["RequestParams.extensionVersion"];
        extensionVersionParam.Should().NotBeNull();
        extensionVersionParam!["examples"]?["1.0.0"].Should().NotBeNull();
        extensionVersionParam["examples"]?["2.0.0"].Should().NotBeNull();

        // Verify Scalar UI endpoint responds
        var scalarResponse = await client.GetAsync("/scalar/v1");
        scalarResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MapBicepExtensionApiExplorer_InProduction_DoesNotExposeOpenApiEndpoint()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Environment.EnvironmentName = Environments.Production;
        builder.WebHost.UseTestServer();
        builder.Services.AddBicepExtensionServices();

        await using var app = builder.Build();
        app.MapBicepExtensionApiExplorer(explorer => explorer.WithTitle("Production API"));

        await app.StartAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/openapi/v2.json");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
