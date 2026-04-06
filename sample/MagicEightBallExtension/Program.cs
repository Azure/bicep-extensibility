// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore;
using Azure.Deployments.Extensibility.AspNetCore.Behaviors;
using MagicEightBallExtension;
using MagicEightBallExtension.Behaviors;
using MagicEightBallExtension.Data;
using MagicEightBallExtension.Handlers;
using Microsoft.AspNetCore.Http.Json;
using V1 = MagicEightBallExtension.Handlers.V1;
using V2 = MagicEightBallExtension.Handlers.V2;

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
app.AddGlobalHandlerBehavior(_ => new PreviewRewriterBehavior(new FakeValueSubstitutionPreviewRewriter()));

// v1 handlers
app.AddExtensionVersion("1.*.*", version => version
    // Version-scoped behavior — validates that the resource API version is 2024-01-01 or 2024-01-01-preview.
    .AddHandlerBehavior(sp => new ApiVersionValidationBehavior("2024-01-01", "2024-01-01-preview"))
    // Generic (default) handler — not scoped to a resource type.
    .AddHandler<FortuneLongRunningOperationGetHandler>()
    // Resource-type-specific handlers for "Fortune".
    .ForResourceType("Fortune", type => type
        .AddHandler<V1.FortunePreviewHandler>()
        .AddHandler<V1.FortuneCreateOrUpdateHandler>()
        .AddHandler<V1.FortuneGetHandler>()
        .AddHandler<V1.FortuneDeleteHandler>()));

// v2 handlers
app.AddExtensionVersion("2.*.*", version => version
    .AddHandlerBehavior(sp => new ApiVersionValidationBehavior("2025-01-01", "2025-01-01-preview")) // 2025-01-01 or 2025-01-01-preview
    .AddHandler<FortuneLongRunningOperationGetHandler>()
    .ForResourceType("Fortune", type => type
        .AddHandler<V2.FortunePreviewHandler>()
        .AddHandler<V2.FortuneCreateOrUpdateHandler>()
        .AddHandler<V2.FortuneGetHandler>()
        .AddHandler<V2.FortuneDeleteHandler>()));

app.EnableDevelopmentScalarApiExplorer(explorer => explorer
    .WithTitle("Magic Eight Ball Extension API")
    .WithExtensionVersions("1.0.0", "2.0.0")
    .ConfigureExamples(FortuneExamples.Configure));

await app.RunAsync();
