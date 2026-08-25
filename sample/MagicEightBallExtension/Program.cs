// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Behaviors;
using Azure.Deployments.Extensibility.AspNetCore.Extensions;
using MagicEightBallExtension;
using MagicEightBallExtension.Behaviors;
using MagicEightBallExtension.Data;
using MagicEightBallExtension.Handlers;
using Microsoft.AspNetCore.Http.Json;
using V1 = MagicEightBallExtension.Handlers.V1;

var builder = WebApplication.CreateBuilder(args);

// Register application services.
builder.Services.AddSingleton<FortuneStore>();
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, FortuneModelSerializerContext.Default);
});

builder.Services.AddBicepExtensionGlobalHandlerBehavior<ResponseLoggingBehavior>();
builder.Services.AddBicepExtensionGlobalHandlerBehavior<NameValidationBehavior>();
builder.Services.AddBicepExtensionGlobalHandlerBehavior(
    _ => new PreviewRewriteBehavior(new FakeValueSubstitutionPreviewRewriter()));

builder.AddBicepExtension(extension => extension
    .AddHandlerBehavior(sp => new ApiVersionValidationBehavior("2024-01-01", "2024-01-01-preview"))
    .AddHandler<FortuneLongRunningOperationGetHandler>()
    .ForResourceType("Fortune", type => type
        .AddHandler<V1.FortunePreviewHandler>()
        .AddHandler<V1.FortuneCreateOrUpdateHandler>()
        .AddHandler<V1.FortuneGetHandler>()
        .AddHandler<V1.FortuneDeleteHandler>()));

var app = builder.Build();
app.UseBicepExtension();
app.MapBicepExtensionApiExplorer(
    FortuneExamples.Configure,
    "Magic Eight Ball Extension API",
    ["1.0.0"]);

await app.RunAsync();
