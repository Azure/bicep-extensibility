// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using Azure.Deployments.Extensibility.Hosting.Managed;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace Azure.Deployments.Extensibility.Hosting.Managed.Tests.Unit;

public class ManagedExtensionHostingTests
{
    [Fact]
    public async Task ManagedExtension_FullPipeline_DispatchesAllContractEndpointsAndPing()
    {
        var metadata = new BicepExtensionMetadata("TestExt", "1.0.0");
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.AddBicepExtension(metadata, extension => extension
            .ForResourceType("Widget", resourceType => resourceType
                .AddHandler<TestPreviewHandler>()
                .AddHandler<TestCreateOrUpdateHandler>()
                .AddHandler<TestGetHandler>()
                .AddHandler<TestDeleteHandler>())
            .AddHandler<TestLroHandler>());

        await using var app = builder.Build();
        app.UseBicepExtension();
        await app.StartAsync();

        var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add("x-ms-client-request-id", "00000000-0000-0000-0000-000000000001");
        client.DefaultRequestHeaders.Add("x-ms-correlation-request-id", "00000000-0000-0000-0000-000000000002");

        // 1. GET /ping
        var pingResponse = await client.GetAsync("/ping");
        pingResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var pingContent = await pingResponse.Content.ReadAsStringAsync();
        pingContent.Should().Be("Healthy");

        // 2. POST /1.0.0/resource/preview
        var previewResponse = await client.PostAsJsonAsync(
            "/1.0.0/resource/preview",
            new ResourcePreviewSpecification
            {
                Type = "Widget",
                Properties = new JsonObject { ["name"] = "testWidget" },
                Metadata = new ResourcePreviewSpecificationMetadata(),
            });
        previewResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var preview = await previewResponse.Content.ReadFromJsonAsync<ResourcePreview>();
        preview.Should().NotBeNull();
        preview!.Type.Should().Be("Widget");

        // 3. POST /1.0.0/resource/createOrUpdate
        var createResponse = await client.PostAsJsonAsync(
            "/1.0.0/resource/createOrUpdate",
            new ResourceSpecification
            {
                Type = "Widget",
                Properties = new JsonObject { ["name"] = "testWidget" },
            });
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var created = await createResponse.Content.ReadFromJsonAsync<Resource>();
        created.Should().NotBeNull();
        created!.Type.Should().Be("Widget");

        // 4. POST /1.0.0/resource/get
        var getResponse = await client.PostAsJsonAsync(
            "/1.0.0/resource/get",
            new ResourceReference
            {
                Type = "Widget",
                Identifiers = new JsonObject { ["name"] = "testWidget" },
            });
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await getResponse.Content.ReadFromJsonAsync<Resource>();
        fetched.Should().NotBeNull();
        fetched!.Type.Should().Be("Widget");

        // 5. POST /1.0.0/resource/delete
        var deleteResponse = await client.PostAsJsonAsync(
            "/1.0.0/resource/delete",
            new ResourceReference
            {
                Type = "Widget",
                Identifiers = new JsonObject { ["name"] = "testWidget" },
            });
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 6. POST /1.0.0/longRunningOperation/get
        var lroResponse = await client.PostAsJsonAsync(
            "/1.0.0/longRunningOperation/get",
            new JsonObject { ["operationId"] = "op-123" });
        lroResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var lro = await lroResponse.Content.ReadFromJsonAsync<LongRunningOperation>();
        lro.Should().NotBeNull();
        lro!.Status.Should().Be("Succeeded");
    }

    [Fact]
    public async Task ManagedExtension_WithMismatchedVersion_ReturnsUnsupportedExtensionVersion()
    {
        var metadata = new BicepExtensionMetadata("TestExt", "1.0.0");
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.AddBicepExtension(metadata, extension => extension
            .ForResourceType("Widget", resourceType => resourceType
                .AddHandler<TestGetHandler>()));

        await using var app = builder.Build();
        app.UseBicepExtension();
        await app.StartAsync();

        var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add("x-ms-client-request-id", "00000000-0000-0000-0000-000000000001");
        client.DefaultRequestHeaders.Add("x-ms-correlation-request-id", "00000000-0000-0000-0000-000000000002");

        var response = await client.PostAsJsonAsync(
            "/2.0.0/resource/get",
            new ResourceReference
            {
                Type = "Widget",
                Identifiers = new JsonObject { ["name"] = "testWidget" },
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error.Should().NotBeNull();
        error!.Error.Code.Should().Be("UnsupportedExtensionVersion");
    }

    [Fact]
    public void AddBicepExtension_CalledTwice_Throws()
    {
        var metadata = new BicepExtensionMetadata("TestExt", "1.0.0");
        var builder = WebApplication.CreateBuilder();

        builder.AddBicepExtension(metadata, extension => extension
            .AddHandler<TestGetHandler>());

        var action = () => builder.AddBicepExtension(metadata, extension => extension
            .AddHandler<TestGetHandler>());

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*Only one Bicep extension registration is supported*");
    }

    [Fact]
    public async Task UseBicepExtension_CalledTwice_Throws()
    {
        var metadata = new BicepExtensionMetadata("TestExt", "1.0.0");
        var builder = WebApplication.CreateBuilder();
        builder.AddBicepExtension(metadata, extension => extension
            .AddHandler<TestGetHandler>());

        await using var app = builder.Build();
        app.UseBicepExtension();

        var action = () => app.UseBicepExtension();

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*UseBicepExtension has already been called*");
    }

    [Fact]
    public void UseBicepExtension_WithoutAddBicepExtension_Throws()
    {
        var builder = WebApplication.CreateBuilder();
        using var app = builder.Build();

        var action = () => app.UseBicepExtension();

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*Call AddBicepExtension before calling UseBicepExtension*");
    }

    [Fact]
    public async Task ManagedExtension_SupportsArbitraryUserServicesMiddlewareAndEndpoints()
    {
        var metadata = new BicepExtensionMetadata("TestExt", "1.0.0");
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        // Custom user service
        builder.Services.AddSingleton<CustomService>();

        builder.AddBicepExtension(metadata, extension => extension
            .ForResourceType("Widget", resourceType => resourceType
                .AddHandler<TestGetHandler>()));

        await using var app = builder.Build();

        // Custom middleware
        app.Use(async (context, next) =>
        {
            context.Response.Headers.Append("X-Custom-Header", "HelloCustom");
            await next(context);
        });

        // Custom endpoint
        app.MapGet("/api/custom", (CustomService service) => service.GetMessage());

        app.UseBicepExtension();
        await app.StartAsync();

        var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add("x-ms-client-request-id", "00000000-0000-0000-0000-000000000001");
        client.DefaultRequestHeaders.Add("x-ms-correlation-request-id", "00000000-0000-0000-0000-000000000002");

        // Test custom endpoint
        var customResponse = await client.GetAsync("/api/custom");
        customResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var customContent = await customResponse.Content.ReadAsStringAsync();
        customContent.Should().Be("custom message");
        customResponse.Headers.GetValues("X-Custom-Header").Should().Contain("HelloCustom");

        // Test extension endpoint alongside custom endpoint
        var extResponse = await client.PostAsJsonAsync(
            "/1.0.0/resource/get",
            new ResourceReference
            {
                Type = "Widget",
                Identifiers = new JsonObject { ["name"] = "testWidget" },
            });
        extResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        extResponse.Headers.GetValues("X-Custom-Header").Should().Contain("HelloCustom");
    }

    [Fact]
    public async Task ManagedExtension_SupportsScalarApiExplorerInDevelopment()
    {
        var metadata = new BicepExtensionMetadata("ScalarTestExt", "1.0.0");
        var builder = WebApplication.CreateBuilder();
        builder.Environment.EnvironmentName = Environments.Development;
        builder.WebHost.UseTestServer();

        builder.AddBicepExtension(metadata, extension => extension
            .ForResourceType("Widget", resourceType => resourceType
                .AddHandler<TestGetHandler>()));

        await using var app = builder.Build();
        app.MapBicepExtensionApiExplorer(explorer => explorer
            .WithTitle("Managed Extension Scalar Test")
            .WithExtensionVersions(metadata.Version));
        app.UseBicepExtension();

        await app.StartAsync();

        var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add("x-ms-client-request-id", "00000000-0000-0000-0000-000000000001");
        client.DefaultRequestHeaders.Add("x-ms-correlation-request-id", "00000000-0000-0000-0000-000000000002");

        // 1. Ping endpoint works
        var pingResponse = await client.GetAsync("/ping");
        pingResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 2. OpenAPI specification endpoint works
        var openApiResponse = await client.GetAsync("/openapi/v2.json");
        openApiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var openApiContent = await openApiResponse.Content.ReadAsStringAsync();
        openApiContent.Should().Contain("Managed Extension Scalar Test");

        // 3. Scalar UI works
        var scalarResponse = await client.GetAsync("/scalar/v1");
        scalarResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. Extension POST route works
        var extResponse = await client.PostAsJsonAsync(
            "/1.0.0/resource/get",
            new ResourceReference
            {
                Type = "Widget",
                Identifiers = new JsonObject { ["name"] = "testWidget" },
            });
        extResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private sealed class CustomService
    {
        public string GetMessage() => "custom message";
    }

    private sealed class TestPreviewHandler : IResourcePreviewHandler
    {
        public Task<OneOf<ResourcePreview, ErrorResponse>> HandleAsync(
            ResourcePreviewSpecification request,
            CancellationToken cancellationToken) =>
            Task.FromResult<OneOf<ResourcePreview, ErrorResponse>>(new ResourcePreview
            {
                Type = request.Type,
                Identifiers = new JsonObject { ["name"] = "testWidget" },
                Properties = request.Properties,
                Metadata = ResourcePreviewMetadata.NewBuilder().Build(),
            });
    }

    private sealed class TestCreateOrUpdateHandler : IResourceCreateOrUpdateHandler
    {
        public Task<OneOf<Resource, LongRunningOperation, ErrorResponse>> HandleAsync(
            ResourceSpecification request,
            CancellationToken cancellationToken) =>
            Task.FromResult<OneOf<Resource, LongRunningOperation, ErrorResponse>>(new Resource
            {
                Type = request.Type,
                Identifiers = new JsonObject { ["name"] = "testWidget" },
                Properties = request.Properties,
            });
    }

    private sealed class TestGetHandler : IResourceGetHandler
    {
        public Task<OneOf<Resource?, ErrorResponse>> HandleAsync(
            ResourceReference request,
            CancellationToken cancellationToken) =>
            Task.FromResult<OneOf<Resource?, ErrorResponse>>(new Resource
            {
                Type = request.Type,
                Identifiers = request.Identifiers,
                Properties = new JsonObject { ["name"] = "testWidget" },
            });
    }

    private sealed class TestDeleteHandler : IResourceDeleteHandler
    {
        public Task<OneOf<Resource?, LongRunningOperation, ErrorResponse>> HandleAsync(
            ResourceReference request,
            CancellationToken cancellationToken) =>
            Task.FromResult<OneOf<Resource?, LongRunningOperation, ErrorResponse>>((Resource?)null);
    }

    private sealed class TestLroHandler : ILongRunningOperationGetHandler
    {
        public Task<OneOf<LongRunningOperation, ErrorResponse>> HandleAsync(
            JsonObject request,
            CancellationToken cancellationToken) =>
            Task.FromResult<OneOf<LongRunningOperation, ErrorResponse>>(
                new LongRunningOperation("Succeeded"));
    }
}
