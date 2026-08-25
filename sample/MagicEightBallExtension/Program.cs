// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore;
using Azure.Deployments.Extensibility.AspNetCore.Behaviors;
using MagicEightBallExtension;
using MagicEightBallExtension.Behaviors;
using MagicEightBallExtension.Data;
using MagicEightBallExtension.Handlers;
using Microsoft.AspNetCore.Http.Json;

var app = ExtensionApplication.Create(args);

// Register application services.
app.ConfigureServices(services =>
{
    services.AddSingleton<FortuneStore>();

    // Demonstrate adding a custom serializer context to the JSON options.
    services.Configure<JsonOptions>(options =>
    {
        options.SerializerOptions.TypeInfoResolverChain.Insert(0, FortuneModelSerializerContext.Default);
    });
});

// Global behaviors — run for every handler invocation.
app.AddGlobalHandlerBehavior<ResponseLoggingBehavior>();
app.AddGlobalHandlerBehavior<NameValidationBehavior>();
app.AddGlobalHandlerBehavior(_ => new PreviewRewriteBehavior(new FakeValueSubstitutionPreviewRewriter()));

// Handlers
app.AddExtensionVersion("1.*.*", version => version
    // Version-scoped behavior — validates that the resource API version is 2024-01-01 or 2024-01-01-preview.
    .AddHandlerBehavior(sp => new ApiVersionValidationBehavior("2024-01-01", "2024-01-01-preview"))
    // Generic (default) handler — not scoped to a resource type.
    .AddHandler<FortuneLongRunningOperationGetHandler>()
    // Resource-type-specific handlers for "Fortune".
    .ForResourceType("Fortune", type => type
        .AddHandler<FortunePreviewHandler>()
        .AddHandler<FortuneCreateOrUpdateHandler>()
        .AddHandler<FortuneGetHandler>()
        .AddHandler<FortuneDeleteHandler>()));

app.EnableDevelopmentScalarApiExplorer(explorer => explorer
    .WithTitle("Magic Eight Ball Extension API")
    .WithExtensionVersions("1.0.0")
    .ConfigureExamples(FortuneExamples.Configure));

await app.RunAsync();
