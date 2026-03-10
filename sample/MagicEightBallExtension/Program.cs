// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore;
using MagicEightBallExtension;
using MagicEightBallExtension.Decorators;
using MagicEightBallExtension.Data;
using MagicEightBallExtension.Handlers;
using Microsoft.AspNetCore.Http.Json;
using V1 = MagicEightBallExtension.Handlers.V1;
using V2 = MagicEightBallExtension.Handlers.V2;
using PipelineV1 = MagicEightBallExtension.Decorators.V1;
using PipelineV2 = MagicEightBallExtension.Decorators.V2;

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

// Global decorators — run for every handler invocation.
app.AddGlobalHandlerDecorator<NameValidationBehavior>();
app.AddGlobalHandlerDecorator<ResponseLoggingBehavior>();
app.AddGlobalHandlerDecorator<PreviewMetadataProcessingBehavior>();

// v1 handlers (extensionVersion >= 1.0.0 and < 2.0.0).
app.AddExtensionVersion(">=1.0.0 <2.0.0", version => version
    // Version-scoped decorator — validates that the resource API version is "2024-01-01".
    .AddHandlerDecorator<PipelineV1.ApiVersionValidationBehavior>()
    // Generic (default) handler — not scoped to a resource type.
    .AddHandler<FortuneLongRunningOperationGetHandler>()
    // Resource-type-specific handlers for "Fortune".
    .ForResourceType("Fortune", type => type
        .AddHandler<V1.FortunePreviewHandler>()
        .AddHandler<V1.FortuneCreateOrUpdateHandler>()
        .AddHandler<V1.FortuneGetHandler>()
        .AddHandler<V1.FortuneDeleteHandler>()));

// v2 handlers (extensionVersion >= 2.0.0).
app.AddExtensionVersion(">=2.0.0", version => version
    .AddHandlerDecorator<PipelineV2.ApiVersionValidationBehavior>()
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
