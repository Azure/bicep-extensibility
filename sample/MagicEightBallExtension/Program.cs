// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore;
using MagicEightBallExtension;
using MagicEightBallExtension.Data;
using MagicEightBallExtension.Handlers;

ExtensionApplication.Create(args)
    .ConfigureServices(services => services.AddSingleton<FortuneStore>())
    .AddHandler<FortunePreviewHandler>()
    .AddHandler<FortuneCreateOrUpdateHandler>()
    .AddHandler<FortuneCreateOrUpdateHandlerV2>()
    .AddHandler<FortuneGetHandler>()
    .AddHandler<FortuneDeleteHandler>()
    .AddHandler<FortuneLongRunningOperationGetHandler>()
    .EnableDevelopmentScalarApiExplorer(FortuneExamples.Configure)
    .Run();
