// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Handlers;
using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using Json.Pointer;
using MagicEightBallExtension.Models;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace MagicEightBallExtension.Handlers.V1;

/// <summary>
/// Previews what a fortune resource would look like without persisting it.
/// Handles unevaluated expressions and produces preview metadata.
/// </summary>
public class FortunePreviewHandler
    : TypedResourcePreviewHandler<FortuneProperties, FortuneIdentifiers>
{
    public FortunePreviewHandler(IOptions<JsonOptions> jsonOptions)
        : base(jsonOptions)
    {
    }

    protected override Task<OneOf<TypedResourcePreview, ErrorResponse>> HandleAsync(
        TypedResourcePreviewSpecification request, CancellationToken cancellationToken)
    {
        var properties = request.Properties;

        // If the "question" property is evaluable, generate a preview fortune.
        if (properties.Question is not null)
        {
            properties = properties with
            {
                Fortune = "Preview: The stars are aligning... (actual fortune generated on create)",
                AnsweredAt = DateTimeOffset.UtcNow.ToString("o"),
            };
        }

        var preview = new TypedResourcePreview
        {
            Type = request.Type,
            ApiVersion = request.ApiVersion,
            Identifiers = new FortuneIdentifiers { Name = properties.Name },
            Properties = properties,
            Config = request.Config,
            ConfigId = request.Config is not null ? "static-config-id" : null,
            Metadata = new ResourcePreviewMetadata
            {
                // "fortune" and "answeredAt" are calculated server-side.
                Calculated = [JsonPointer.Parse("/properties/fortune"),
                              JsonPointer.Parse("/properties/answeredAt")],
                // "fortune" is read-only — the user can't set it.
                ReadOnly = [JsonPointer.Parse("/properties/fortune")],
                // Echo back any unevaluated paths from the request.
                Unevaluated = request.Metadata?.Unevaluated,
            },
        };

        return Task.FromResult<OneOf<TypedResourcePreview, ErrorResponse>>(preview);
    }
}
