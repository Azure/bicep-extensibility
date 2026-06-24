// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using Azure.Deployments.Extensibility.Hosting.Managed;
using BlobPocExtension.Handlers;
using System.Text.Json.Nodes;

BicepExtension.CreateBuilder(args)            // opinionated defaults applied here
    .AddExtensionVersion("1.0.0", v => v      // exact version only (no range/wildcard)
        .AddHandler<BlobLongRunningOperationGetHandler>()   // generic (not resource-type scoped)
        .ForResourceType("Blob", t => t
            .AddHandler<BlobPreviewHandler>()
            .AddHandler<BlobCreateOrUpdateHandler>()
            .AddHandler<BlobGetHandler>()
            .AddHandler<BlobDeleteHandler>()))
    .ConfigureApiExplorerExamples(examples =>
    {
        const string apiVersion = "2024-01-01";

        var identifiers = new JsonObject
        {
            ["accountName"] = "acct",
            ["containerName"] = "c1",
            ["blobName"] = "b1",
        };

        var resource = new Resource
        {
            Type = "Blob",
            ApiVersion = apiVersion,
            Identifiers = identifiers.DeepClone().AsObject(),
            Properties = new JsonObject
            {
                ["accountName"] = "acct",
                ["containerName"] = "c1",
                ["blobName"] = "b1",
                ["content"] = "hello from the POC",
                ["contentType"] = "text/plain",
            },
        };

        examples
            .ForCreateOrUpdate(
                request: new ResourceSpecification
                {
                    Type = "Blob",
                    ApiVersion = apiVersion,
                    Properties = new JsonObject
                    {
                        ["accountName"] = "acct",
                        ["containerName"] = "c1",
                        ["blobName"] = "b1",
                        ["content"] = "hello from the POC",
                        ["contentType"] = "text/plain",
                    },
                },
                response: resource)
            .ForGet(
                request: new ResourceReference
                {
                    Type = "Blob",
                    ApiVersion = apiVersion,
                    Identifiers = identifiers.DeepClone().AsObject(),
                },
                response: resource)
            .ForDelete(
                request: new ResourceReference
                {
                    Type = "Blob",
                    ApiVersion = apiVersion,
                    Identifiers = identifiers.DeepClone().AsObject(),
                })
            .ForPreview(
                request: new ResourcePreviewSpecification
                {
                    Type = "Blob",
                    ApiVersion = apiVersion,
                    Properties = new JsonObject
                    {
                        ["accountName"] = "acct",
                        ["containerName"] = "c1",
                        ["blobName"] = "b1",
                        ["content"] = "hello from the POC",
                        ["contentType"] = "text/plain",
                    },
                    Metadata = new ResourcePreviewSpecificationMetadata
                    {
                        Unevaluated = [],
                    },
                },
                response: new ResourcePreview
                {
                    Type = "Blob",
                    ApiVersion = apiVersion,
                    Identifiers = identifiers.DeepClone().AsObject(),
                    Properties = new JsonObject
                    {
                        ["accountName"] = "acct",
                        ["containerName"] = "c1",
                        ["blobName"] = "b1",
                        ["content"] = "hello from the POC",
                        ["contentType"] = "text/plain",
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
