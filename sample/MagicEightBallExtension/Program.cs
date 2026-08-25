// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore;
using Azure.Deployments.Extensibility.AspNetCore.Behaviors;
using Azure.Deployments.Extensibility.Hosting.Managed.Extensions;
using MagicEightBallExtension;
using MagicEightBallExtension.Behaviors;
using MagicEightBallExtension.Data;
using MagicEightBallExtension.Handlers;
using Microsoft.AspNetCore.Http.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<FortuneStore>();
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, FortuneModelSerializerContext.Default);
});

builder.Services.AddBicepExtension(extension => extension
    .AddGlobalHandlerBehavior<ResponseLoggingBehavior>()
    .AddGlobalHandlerBehavior<NameValidationBehavior>()
    .AddGlobalHandlerBehavior(_ => new PreviewRewriteBehavior(new FakeValueSubstitutionPreviewRewriter()))
    .AddHandlerBehavior(_ => new ApiVersionValidationBehavior("2025-01-01", "2025-01-01-preview"))
    .AddHandler<FortuneLongRunningOperationGetHandler>()
    .ForResourceType("Fortune", type => type
        .AddHandler<FortunePreviewHandler>()
        .AddHandler<FortuneCreateOrUpdateHandler>()
        .AddHandler<FortuneGetHandler>()
        .AddHandler<FortuneDeleteHandler>()));

var app = builder.Build();

app.MapBicepExtension();
app.MapManagedScalarApiExplorer(explorer => explorer
    .WithTitle("Magic Eight Ball Extension API")
    .ConfigureExamples(FortuneExamples.Configure));

await app.RunAsync();
