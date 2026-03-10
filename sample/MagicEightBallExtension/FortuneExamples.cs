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
        ConfigureV1Examples(examples);
        ConfigureV2Examples(examples);
    }

    private static void ConfigureV1Examples(OpenApiExamplesBuilder examples)
    {
        const string ExampleName = "v1 (1.0.0)";
        const string ApiVersion = "2024-01-01";

        var identifiers = new JsonObject { ["name"] = "my-fortune" };

        var resource = new Resource
        {
            Type = "Fortune",
            ApiVersion = ApiVersion,
            Identifiers = identifiers,
            Properties = new JsonObject
            {
                ["name"] = "my-fortune",
                ["question"] = "Will I be lucky today?",
                ["fortune"] = "Signs point to yes",
                ["answeredAt"] = "2024-10-01T12:00:00.0000000+00:00",
            },
        };

        var reference = new ResourceReference
        {
            Type = "Fortune",
            ApiVersion = ApiVersion,
            Identifiers = identifiers.DeepClone().AsObject(),
        };

        examples
            .ForPreview(ExampleName,
                request: new ResourcePreviewSpecification
                {
                    Type = "Fortune",
                    ApiVersion = ApiVersion,
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
                    ApiVersion = ApiVersion,
                    Identifiers = identifiers.DeepClone().AsObject(),
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
            .ForCreateOrUpdate(ExampleName,
                request: new ResourceSpecification
                {
                    Type = "Fortune",
                    ApiVersion = ApiVersion,
                    Properties = new JsonObject
                    {
                        ["name"] = "my-fortune",
                        ["question"] = "Will I be lucky today?",
                    },
                },
                response: resource)
            .ForGet(ExampleName,
                request: reference,
                response: resource)
            .ForDelete(ExampleName,
                request: new ResourceReference
                {
                    Type = "Fortune",
                    ApiVersion = ApiVersion,
                    Identifiers = identifiers.DeepClone().AsObject(),
                })
            .ForLongRunningOperationGet(ExampleName,
                request: new JsonObject { ["operationId"] = "op-abc123" },
                response: new LongRunningOperation
                {
                    Status = "Succeeded",
                });
    }

    private static void ConfigureV2Examples(OpenApiExamplesBuilder examples)
    {
        const string ExampleName = "v2 (2.0.0)";
        const string ApiVersion = "2025-01-01";

        var identifiers = new JsonObject { ["name"] = "my-fortune" };

        var resource = new Resource
        {
            Type = "Fortune",
            ApiVersion = ApiVersion,
            Identifiers = identifiers,
            Properties = new JsonObject
            {
                ["name"] = "my-fortune",
                ["question"] = "Will I be lucky today?",
                ["fortune"] = "Signs point to yes",
                ["answeredAt"] = "2025-10-01T12:00:00.0000000+00:00",
                ["confidence"] = 85,
                ["mood"] = "Cosmic",
            },
        };

        var reference = new ResourceReference
        {
            Type = "Fortune",
            ApiVersion = ApiVersion,
            Identifiers = identifiers.DeepClone().AsObject(),
        };

        examples
            .ForPreview(ExampleName,
                request: new ResourcePreviewSpecification
                {
                    Type = "Fortune",
                    ApiVersion = ApiVersion,
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
                    ApiVersion = ApiVersion,
                    Identifiers = identifiers.DeepClone().AsObject(),
                    Properties = new JsonObject
                    {
                        ["name"] = "my-fortune",
                        ["question"] = "Will I be lucky today?",
                        ["fortune"] = "<calculated at deployment time>",
                        ["answeredAt"] = "<calculated at deployment time>",
                        ["confidence"] = "<calculated at deployment time>",
                        ["mood"] = "<calculated at deployment time>",
                    },
                    Metadata = new ResourcePreviewMetadata
                    {
                        Calculated = [
                            JsonPointer.Parse("/properties/fortune"),
                            JsonPointer.Parse("/properties/answeredAt"),
                            JsonPointer.Parse("/properties/confidence"),
                            JsonPointer.Parse("/properties/mood")],
                        ReadOnly = [
                            JsonPointer.Parse("/properties/fortune"),
                            JsonPointer.Parse("/properties/answeredAt"),
                            JsonPointer.Parse("/properties/confidence"),
                            JsonPointer.Parse("/properties/mood")],
                    },
                })
            .ForCreateOrUpdate(ExampleName,
                request: new ResourceSpecification
                {
                    Type = "Fortune",
                    ApiVersion = ApiVersion,
                    Properties = new JsonObject
                    {
                        ["name"] = "my-fortune",
                        ["question"] = "Will I be lucky today?",
                    },
                },
                response: resource)
            .ForGet(ExampleName,
                request: reference,
                response: resource)
            .ForDelete(ExampleName,
                request: new ResourceReference
                {
                    Type = "Fortune",
                    ApiVersion = ApiVersion,
                    Identifiers = identifiers.DeepClone().AsObject(),
                })
            .ForLongRunningOperationGet(ExampleName,
                request: new JsonObject { ["operationId"] = "op-abc123" },
                response: new LongRunningOperation
                {
                    Status = "Succeeded",
                });
    }
}
