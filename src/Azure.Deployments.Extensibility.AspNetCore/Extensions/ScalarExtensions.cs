// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Builders;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.AspNetCore.Extensions;

/// <summary>
/// Extension methods for adding the Scalar API explorer to an extensibility application.
/// </summary>
internal static class ScalarExtensions
{
    private const string OpenApiDocumentName = "v2";
    private const string DefaultServerUrl = "http://localhost:8080";
    internal const string DefaultTitle = "Bicep Extensibility Provider API";

    /// <summary>
    /// Maps the Scalar API explorer UI and the OpenAPI specification endpoint.
    /// Only registers routes when the application is running in the Development environment.
    /// </summary>
    /// <example>
    /// <code>
    /// var app = builder.Build();
    /// app.UseDevelopmentApiExplorer(examples =>
    /// {
    ///     examples.ForCreateOrUpdate(
    ///         request: new { type = "MyResource", properties = new { name = "example" } },
    ///         response: new { type = "MyResource", identifiers = new { name = "example" }, properties = new { name = "example" } });
    /// });
    /// app.MapResourceActions();
    /// app.MapLongRunningOperationActions();
    /// app.Run();
    /// </code>
    /// </example>
    public static WebApplication MapDevelopmentScalarApiExplorer(
        this WebApplication app,
        Action<OpenApiExamplesBuilder>? configureExamples = null,
        string title = DefaultTitle,
        string[]? extensionVersions = null)
    {
        if (!app.Environment.IsDevelopment())
        {
            return app;
        }

        var openApiJson = BuildOpenApiDocument(configureExamples, extensionVersions);

        app.MapGet($"/openapi/{OpenApiDocumentName}.json", (HttpRequest request) =>
        {
            var serverUrl = $"{request.Scheme}://{request.Host}";
            var json = openApiJson.Replace(DefaultServerUrl, serverUrl);

            return Results.Text(json, "application/json");
        }).ExcludeFromDescription();

        app.MapScalarApiReference(options =>
        {
            options
                .WithTitle(title)
                .WithOpenApiRoutePattern($"/openapi/{OpenApiDocumentName}.json");
        });

        return app;
    }

    private static string BuildOpenApiDocument(Action<OpenApiExamplesBuilder>? configureExamples, string[]? extensionVersions)
    {
        var assembly = typeof(ScalarExtensions).Assembly;
        using var stream = assembly.GetManifestResourceStream("openapi.json")
            ?? throw new InvalidOperationException("Embedded OpenAPI specification not found.");

        var document = JsonNode.Parse(stream)
            ?? throw new InvalidOperationException("Failed to parse OpenAPI specification.");

        InjectParameterExamples(document, extensionVersions);

        if (configureExamples is not null)
        {
            var builder = new OpenApiExamplesBuilder();
            configureExamples(builder);
            InjectOperationExamples(document, builder);
        }

        return document.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    private static void InjectParameterExamples(JsonNode document, string[]? extensionVersions)
    {
        var parameters = document["components"]?["parameters"]?.AsObject();

        if (parameters is null)
        {
            return;
        }

        var headerExamples = new Dictionary<string, string>
        {
            ["RequestHeaders.clientRequestId"] = "00000000-0000-0000-0000-000000000000",
            ["RequestHeaders.correlationRequestId"] = "00000000-0000-0000-0000-000000000000",
            ["RequestHeaders.clientTenantId"] = "00000000-0000-0000-0000-000000000000",
            ["RequestHeaders.homeTenantId"] = "00000000-0000-0000-0000-000000000000",
            ["RequestHeaders.acceptLanguage"] = "en-US",
            ["RequestHeaders.referer"] = "http://localhost",
            ["RequestHeaders.traceparent"] = "00-00000000000000000000000000000000-0000000000000000-00",
            ["RequestHeaders.tracestate"] = "ext=00000000000000000000000000000000",
        };

        foreach (var (paramName, example) in headerExamples)
        {
            if (parameters[paramName] is JsonObject param)
            {
                param["example"] = example;
            }
        }

        // Inject extensionVersion examples as a named map so Scalar renders a dropdown.
        if (parameters["RequestParams.extensionVersion"] is JsonObject versionParam)
        {
            var versions = extensionVersions is { Length: > 0 } ? extensionVersions : ["1.0.0"];

            if (versions.Length == 1)
            {
                versionParam["example"] = versions[0];
            }
            else
            {
                var versionExamples = new JsonObject();

                foreach (var version in versions)
                {
                    versionExamples[version] = new JsonObject { ["value"] = version };
                }

                versionParam["examples"] = versionExamples;
            }
        }
    }

    private static void InjectOperationExamples(JsonNode document, OpenApiExamplesBuilder builder)
    {
        var paths = document["paths"]?.AsObject();

        if (paths is null)
        {
            return;
        }

        foreach (var (_, pathItem) in paths)
        {
            var postOperation = pathItem?["post"];
            var operationId = postOperation?["operationId"]?.GetValue<string>();

            if (operationId is null || !builder.Operations.TryGetValue(operationId, out var opExamples))
            {
                continue;
            }

            var requestExamples = new JsonObject();
            var responseExamples = new JsonObject();

            foreach (var (exampleName, example) in opExamples.Examples)
            {
                if (example.Request is not null)
                {
                    requestExamples[exampleName] = new JsonObject
                    {
                        ["value"] = JsonSerializer.SerializeToNode(example.Request),
                    };
                }

                if (example.Response is not null)
                {
                    responseExamples[exampleName] = new JsonObject
                    {
                        ["value"] = JsonSerializer.SerializeToNode(example.Response),
                    };
                }
            }

            if (requestExamples.Count > 0)
            {
                var requestContent = postOperation!["requestBody"]?["content"]?["application/json"];

                if (requestContent is JsonObject requestContentObj)
                {
                    requestContentObj["examples"] = requestExamples;
                }
            }

            if (responseExamples.Count > 0)
            {
                var responseContent = postOperation!["responses"]?[opExamples.ResponseStatusCode]?["content"]?["application/json"];

                if (responseContent is JsonObject responseContentObj)
                {
                    responseContentObj["examples"] = responseExamples;
                }
            }
        }
    }
}
