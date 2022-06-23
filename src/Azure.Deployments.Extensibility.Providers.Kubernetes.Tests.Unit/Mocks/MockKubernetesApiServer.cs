// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core;
using Azure.Deployments.Extensibility.Providers.Kubernetes.Models;
using Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Unit.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit.Abstractions;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Unit.Mocks
{
    public sealed class MockKubernetesApiServer : IAsyncDisposable
    {
        private readonly WebApplication app;

        private MockKubernetesApiServer(WebApplication app)
        {
            this.app = app;
        }

        public static Task<MockKubernetesApiServer> StartAsync(ITestOutputHelper testOutput, params RequestDelegate[] requestHandlerSequence) =>
            StartAsync(testOutput, (IEnumerable<RequestDelegate>)requestHandlerSequence);

        public static Task<MockKubernetesApiServer> StartAsync(ITestOutputHelper testOutput, IEnumerable<RequestDelegate> requestHandlerSequence)
        {
            var requestHandlerEnumerator = requestHandlerSequence.GetEnumerator();

            return StartAsync(testOutput, async httpContext =>
            {
                if (requestHandlerEnumerator.MoveNext())
                {
                    await requestHandlerEnumerator.Current.Invoke(httpContext);
                }
                else
                {
                    throw new IndexOutOfRangeException("No request handler for the current request.");
                }
            });
        }

        public static async Task<MockKubernetesApiServer> StartAsync(ITestOutputHelper testOutput, RequestDelegate requestHandler)
        {
            var builder = WebApplication.CreateBuilder();

            builder.Services.Configure<JsonOptions>(options =>
            {
                options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            });

            builder.Logging.ClearProviders();
            builder.Logging.AddProvider(new TestOutputLoggerProvider(testOutput));

            var app = builder.Build();

            // Use dynamic port to avoid conflicts among tests.
            app.Urls.Add($"http://{IPAddress.Loopback}:0");

            app.Run(requestHandler);

            await app.StartAsync();

            return new(app);
        }

        public ExtensibilityOperationRequest InjectKubeConfig(ExtensibilityOperationRequest request)
        {
            var import = ModelMapper.MapToConcrete<KubernetesConfig>(request.Import);

            import = import with
            {
                Config = import.Config with
                {
                    KubeConfig = Encoding.UTF8.GetBytes($@"
apiVersion: v1
clusters:
- cluster:
    server: {this.app.Urls.First()}
  name: test-cluster
contexts:
- context:
    cluster: test-cluster
  name: test-context
current-context: test-context
kind: Config
"),
                },
            };

            return request with
            {
                Import = ModelMapper.MapToGeneric(import),
            };
        }

        public async ValueTask DisposeAsync()
        {
            await this.app.StopAsync();
            await this.app.DisposeAsync();
        }
    }
}
