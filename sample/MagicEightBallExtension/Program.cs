// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore;
using MagicEightBallExtension;
using MagicEightBallExtension.Pipeline;
using MagicEightBallExtension.Data;
using MagicEightBallExtension.Handlers;
using Microsoft.AspNetCore.Http.Json;
using V1 = MagicEightBallExtension.Handlers.V1;
using V2 = MagicEightBallExtension.Handlers.V2;
using PipelineV1 = MagicEightBallExtension.Pipeline.V1;
using PipelineV2 = MagicEightBallExtension.Pipeline.V2;

ExtensionApplication.Create(args)
    // Register application services.
    .ConfigureServices(services =>
    {
        services.AddSingleton<FortuneStore>();

        // Demonstrate adding a custom serializer context to the JSON options.
        services.Configure<JsonOptions>(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, FortuneModelSerializerContext.Default);
        });
    })

    // Global pipeline behaviors — run for every handler invocation.
    .AddGlobalPipelineBehavior<NameValidationBehavior>()
    .AddGlobalPipelineBehavior<ResponseLoggingBehavior>()
    .AddGlobalPipelineBehavior<PreviewMetadataProcessingBehavior>()

    // v1 handlers (extensionVersion >= 1.0.0 and < 2.0.0).
    .AddExtensionVersion(">=1.0.0 <2.0.0", version => version
        // Version-scoped behavior — validates that the resource API version is "2024-01-01".
        .AddPipelineBehavior<PipelineV1.ApiVersionValidationBehavior>()
        // Generic (default) handler — not scoped to a resource type.
        .AddHandler<FortuneLongRunningOperationGetHandler>()
        // Resource-type-specific handlers for "Fortune".
        .ForResourceType("Fortune", type => type
            .AddHandler<V1.FortunePreviewHandler>()
            .AddHandler<V1.FortuneCreateOrUpdateHandler>()
            .AddHandler<V1.FortuneGetHandler>()
            .AddHandler<V1.FortuneDeleteHandler>()))

    // v2 handlers (extensionVersion >= 2.0.0).
    .AddExtensionVersion(">=2.0.0", version => version
        .AddPipelineBehavior<PipelineV2.ApiVersionValidationBehavior>()
        .AddHandler<FortuneLongRunningOperationGetHandler>()
        .ForResourceType("Fortune", type => type
            .AddHandler<V2.FortunePreviewHandler>()
            .AddHandler<V2.FortuneCreateOrUpdateHandler>()
            .AddHandler<V2.FortuneGetHandler>()
            .AddHandler<V2.FortuneDeleteHandler>()))

    .EnableDevelopmentScalarApiExplorer(
        FortuneExamples.Configure,
        title: "Magic Eight Ball Extension API",
        extensionVersions: ["1.0.0", "2.0.0"])
    .Run();
