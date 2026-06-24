// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Hosting.Managed;
using BlobPocExtension.Handlers;

BicepExtension.CreateBuilder(args)            // opinionated defaults applied here
    .AddExtensionVersion("1.0.0", v => v      // exact version only (no range/wildcard)
        .ForResourceType("Blob", t => t
            .AddHandler<BlobCreateOrUpdateHandler>()
            .AddHandler<BlobGetHandler>()
            .AddHandler<BlobDeleteHandler>()))
    .Run();
