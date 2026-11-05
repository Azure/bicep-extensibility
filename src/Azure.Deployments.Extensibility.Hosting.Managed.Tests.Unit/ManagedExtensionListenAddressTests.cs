// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using Azure.Deployments.Extensibility.Hosting.Managed.Extensions;
using Azure.Deployments.Extensibility.Hosting.Managed.Metadata;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace Azure.Deployments.Extensibility.Hosting.Managed.Tests.Unit;

public class ManagedExtensionListenAddressTests
{
    private static readonly BicepExtensionDescriptor Descriptor = new("Widget", "1.0.0");

    [Fact]
    public async Task Start_WithExtensionPort_BindsLoopbackPortInsteadOfUrls()
    {
        var port = GetFreePort();
        await using var app = BuildApplication(Invariant(port), host: null);

        await app.StartAsync();

        app.Urls.Should().Equal($"http://127.0.0.1:{port}");
        var status = await PingAsync($"http://127.0.0.1:{port}");
        status.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Start_WithExtensionHostAndPort_BindsThatAddress()
    {
        var port = GetFreePort();
        await using var app = BuildApplication(Invariant(port), host: "0.0.0.0");

        await app.StartAsync();

        app.Urls.Should().Equal($"http://0.0.0.0:{port}");
        var status = await PingAsync($"http://127.0.0.1:{port}");
        status.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Start_WithoutListenVariables_KeepsAspNetCoreUrls()
    {
        await using var app = BuildApplication(port: null, host: null);

        await app.StartAsync();

        var url = app.Urls.Should().ContainSingle().Subject;
        url.Should().StartWith("http://127.0.0.1:").And.NotEndWith(":0");
        var status = await PingAsync(url);
        status.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Start_WithAppConfiguredKestrelEndpoint_KeepsItAlongsideExtensionPort()
    {
        var extensionPort = GetFreePort();
        var appPort = GetFreePort();
        var builder = CreateBuilder(
            Invariant(extensionPort),
            host: null,
            options => options.Listen(IPAddress.Loopback, appPort));
        await using var app = BuildApplication(builder);

        await app.StartAsync();

        app.Urls.Should().BeEquivalentTo($"http://127.0.0.1:{extensionPort}", $"http://127.0.0.1:{appPort}");
    }

    [Fact]
    public async Task Start_WithHttpsEndpointDefaults_KeepsExtensionEndpointHttpAndAppEndpointHttps()
    {
        var extensionPort = GetFreePort();
        var appPort = GetFreePort();
        using var certificate = CreateSelfSignedCertificate();
        var builder = CreateBuilder(
            Invariant(extensionPort),
            host: null,
            options =>
            {
                options.ConfigureEndpointDefaults(endpoint => endpoint.UseHttps(certificate));
                options.Listen(IPAddress.Loopback, appPort);
            });
        await using var app = BuildApplication(builder);

        await app.StartAsync();

        app.Urls.Should().HaveCount(2)
            .And.Contain($"http://127.0.0.1:{extensionPort}")
            .And.Contain($"https://127.0.0.1:{appPort}");
        var status = await PingAsync($"http://127.0.0.1:{extensionPort}");
        status.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("65536")]
    [InlineData("80.5")]
    [InlineData(" 8080")]
    public async Task Start_InvalidExtensionPort_FailsNamingTheVariable(string port)
    {
        var action = () => BuildAndStartAsync(port, host: null);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*EXTENSION_PORT*'{port}'*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("localhost")]
    [InlineData("example.com")]
    [InlineData("not an address")]
    public async Task Start_InvalidExtensionHost_FailsNamingTheVariable(string host)
    {
        var action = () => BuildAndStartAsync(Invariant(GetFreePort()), host);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*EXTENSION_HOST*'{host}'*");
    }

    [Fact]
    public async Task Start_ExtensionHostWithoutPort_Fails()
    {
        var action = () => BuildAndStartAsync(port: null, host: "0.0.0.0");

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*EXTENSION_HOST*EXTENSION_PORT*");
    }

    private static WebApplication BuildApplication(string? port, string? host) =>
        BuildApplication(CreateBuilder(port, host));

    private static WebApplicationBuilder CreateBuilder(
        string? port,
        string? host,
        Action<KestrelServerOptions>? configureKestrelBeforeExtension = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        // Null entries also hide any listen variables set in the test process environment.
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["EXTENSION_PORT"] = port,
            ["EXTENSION_HOST"] = host,
        });
        if (configureKestrelBeforeExtension is not null)
        {
            builder.WebHost.ConfigureKestrel(configureKestrelBeforeExtension);
        }

        builder.Services.AddBicepExtension(extension => extension.AddHandler<GetHandler>(), Descriptor);

        return builder;
    }

    private static WebApplication BuildApplication(WebApplicationBuilder builder)
    {
        var app = builder.Build();
        app.UseBicepExtension();

        return app;
    }

    // Kestrel reads its options when the application is built, before StartAsync.
    private static async Task BuildAndStartAsync(string? port, string? host)
    {
        await using var app = BuildApplication(port, host);
        await app.StartAsync();
    }

    private static async Task<HttpStatusCode> PingAsync(string baseAddress)
    {
        using var client = new HttpClient { BaseAddress = new Uri(baseAddress) };
        using var response = await client.GetAsync("/ping");

        return response.StatusCode;
    }

    private static int GetFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static string Invariant(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static X509Certificate2 CreateSelfSignedCertificate()
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=localhost", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var ephemeral = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddHours(1));

        // Reloading from PFX gives Windows TLS a usable private key; ephemeral keys fail there.
        return X509CertificateLoader.LoadPkcs12(ephemeral.Export(X509ContentType.Pfx), password: null);
    }

    private sealed class GetHandler : IResourceGetHandler
    {
        public Task<OneOf<Resource?, ErrorResponse>> HandleAsync(
            ResourceReference request,
            CancellationToken cancellationToken) =>
            Task.FromResult<OneOf<Resource?, ErrorResponse>>((Resource?)null);
    }
}
