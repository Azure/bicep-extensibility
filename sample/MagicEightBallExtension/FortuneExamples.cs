// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Builders;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using Json.Pointer;
using System.Text.Json.Nodes;

namespace MagicEightBallExtension;

/// <summary>
/// Configures OpenAPI examples for the Scalar API explorer.
/// </summary>
internal static class FortuneExamples
{
    public static void Configure(OpenApiExamplesBuilder examples)
    {
        var sampleProperties = new JsonObject
        {
            ["name"] = "my-fortune",
            ["question"] = "Will I be lucky today?",
            ["fortune"] = "Signs point to yes",
            ["answeredAt"] = "2026-03-06T12:00:00.0000000+00:00",
        };

        var sampleIdentifiers = new JsonObject { ["name"] = "my-fortune" };

        var sampleResource = new Resource
        {
            Type = "Fortune",
            ApiVersion = "2026-01-01",
            Identifiers = sampleIdentifiers,
            Properties = sampleProperties,
        };

        var sampleReference = new ResourceReference
        {
            Type = "Fortune",
            ApiVersion = "2026-01-01",
            Identifiers = sampleIdentifiers.DeepClone().AsObject(),
        };

        examples
            .ForPreview(
                request: new ResourcePreviewSpecification
                {
                    Type = "Fortune",
                    ApiVersion = "2026-01-01",
                    Properties = new JsonObject
                    {
                        ["name"] = "my-fortune",
                        ["question"] = "Will I be lucky today?",
                    },
                    Metadata = new ResourcePreviewSpecificationMetadata
                    {
                        Unevaluated = [],
                    },
                },
                response: new ResourcePreview
                {
                    Type = "Fortune",
                    ApiVersion = "2026-01-01",
                    Identifiers = sampleIdentifiers.DeepClone().AsObject(),
                    Properties = new JsonObject
                    {
                        ["name"] = "my-fortune",
                        ["question"] = "Will I be lucky today?",
                        ["fortune"] = "<calculated at deployment time>",
                        ["answeredAt"] = "<calculated at deployment time>",
                    },
                    Metadata = new ResourcePreviewMetadata
                    {
                        Calculated = [JsonPointer.Parse("/properties/fortune"), JsonPointer.Parse("/properties/answeredAt")],
                        ReadOnly = [JsonPointer.Parse("/properties/fortune"), JsonPointer.Parse("/properties/answeredAt")],
                    },
                })
            .ForCreateOrUpdate(
                request: new ResourceSpecification
                {
                    Type = "Fortune",
                    ApiVersion = "2026-01-01",
                    Properties = new JsonObject
                    {
                        ["name"] = "my-fortune",
                        ["question"] = "Will I be lucky today?",
                    },
                },
                response: sampleResource)
            .ForGet(
                request: sampleReference,
                response: sampleResource)
            .ForDelete(
                request: new ResourceReference
                {
                    Type = "Fortune",
                    ApiVersion = "2026-01-01",
                    Identifiers = sampleIdentifiers.DeepClone().AsObject(),
                })
            .ForLongRunningOperationGet(
                request: new JsonObject { ["operationId"] = "op-abc123" },
                response: new LongRunningOperation
                {
                    Status = "Succeeded",
                });
    }
}
