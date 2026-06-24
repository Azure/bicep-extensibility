// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Core;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using Azure.Deployments.Extensibility.Hosting.Managed;
using Azure.Identity;
using BlobPocExtension.Behaviors;
using BlobPocExtension.Handlers;
using BlobPocExtension.Storage;
using System.Text.Json.Nodes;

BicepExtension.CreateBuilder(args)            // opinionated defaults applied here
    .ConfigureServices(services =>
    {
        // DefaultAzureCredential resolves the developer's `az login` credential locally and a
        // managed identity in the cloud — same auth path everywhere, no secrets in code/config.
        services.AddSingleton<TokenCredential>(_ => new DefaultAzureCredential());
        services.AddOptions<BlobPocOptions>().BindConfiguration("Storage");
        services.AddSingleton<IStorageClientFactory, StorageClientFactory>();
    })
    .AddExtensionVersion("1.0.0", v => v      // exact version only (no range/wildcard)
        .AddHandlerBehavior<StorageExceptionHandlingBehavior>()             // maps RequestFailedException -> ErrorResponse
        .AddHandlerBehavior(_ => new ApiVersionValidationBehavior("2024-01-01")) // rejects unsupported apiVersion
        .AddHandlerBehavior<AccountNameValidationBehavior>()                // validates accountName before handlers run
        .AddHandler<BlobLongRunningOperationGetHandler>()   // generic stub; container ops are synchronous
        .ForResourceType("Container", t => t
            .AddHandler<ContainerPreviewHandler>()
            .AddHandler<ContainerCreateOrUpdateHandler>()
            .AddHandler<ContainerGetHandler>()
            .AddHandler<ContainerDeleteHandler>()))
    .AddHealthCheck<StorageHealthCheck>("storage")
    .ConfigureApiExplorerExamples(examples =>
    {
        const string apiVersion = "2024-01-01";

        var identifiers = new JsonObject
        {
            ["accountName"] = "acct",
            ["containerName"] = "c1",
        };

        var resource = new Resource
        {
            Type = "Container",
            ApiVersion = apiVersion,
            Identifiers = identifiers.DeepClone().AsObject(),
            Properties = new JsonObject
            {
                ["accountName"] = "acct",
                ["containerName"] = "c1",
                ["publicAccess"] = "none",
            },
        };

        examples
            .ForCreateOrUpdate(
                request: new ResourceSpecification
                {
                    Type = "Container",
                    ApiVersion = apiVersion,
                    Properties = new JsonObject
                    {
                        ["accountName"] = "acct",
                        ["containerName"] = "c1",
                        ["publicAccess"] = "none",
                    },
                },
                response: resource)
            .ForGet(
                request: new ResourceReference
                {
                    Type = "Container",
                    ApiVersion = apiVersion,
                    Identifiers = identifiers.DeepClone().AsObject(),
                },
                response: resource)
            .ForDelete(
                request: new ResourceReference
                {
                    Type = "Container",
                    ApiVersion = apiVersion,
                    Identifiers = identifiers.DeepClone().AsObject(),
                })
            .ForPreview(
                request: new ResourcePreviewSpecification
                {
                    Type = "Container",
                    ApiVersion = apiVersion,
                    Properties = new JsonObject
                    {
                        ["accountName"] = "acct",
                        ["containerName"] = "c1",
                    },
                    Metadata = new ResourcePreviewSpecificationMetadata
                    {
                        Unevaluated = [],
                    },
                },
                response: new ResourcePreview
                {
                    Type = "Container",
                    ApiVersion = apiVersion,
                    Identifiers = identifiers.DeepClone().AsObject(),
                    Properties = new JsonObject
                    {
                        ["accountName"] = "acct",
                        ["containerName"] = "c1",
                        ["publicAccess"] = "none",
                    },
                    Metadata = new ResourcePreviewMetadata
                    {
                        Calculated = [],
                        ReadOnly = [],
                    },
                })
            .ForLongRunningOperationGet(
                request: new JsonObject { ["operationId"] = "op-abc123" },
                response: new LongRunningOperation
                {
                    Status = "Succeeded",
                });
    })
    .Run();
