// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Handlers;
using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using Json.Pointer;
using MagicEightBallExtension.Models;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace MagicEightBallExtension.Handlers.V2;

/// <summary>
/// Previews what a v2 fortune resource would look like without persisting it.
/// Extends the v1 preview with placeholder "confidence" and "mood" values,
/// and marks all four server-computed fields in the preview metadata.
/// </summary>
public class FortunePreviewHandler
    : TypedResourcePreviewHandler<FortunePropertiesV2, FortuneIdentifiers>
{
    public FortunePreviewHandler(IOptions<JsonOptions> jsonOptions)
        : base(jsonOptions)
    {
    }

    protected override Task<OneOf<TypedResourcePreview, ErrorResponse>> HandleAsync(
        TypedResourcePreviewSpecification request, CancellationToken cancellationToken)
    {
        var properties = request.Properties;

        // If the "question" property is evaluable, generate a preview fortune with placeholder computed values.
        if (properties.Question is not null)
        {
            properties = properties with
            {
                Fortune = "Preview: The stars are aligning... (actual fortune generated on create)",
                Confidence = 42,
                Mood = "Cosmic",
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
                // "fortune", "answeredAt", "confidence", and "mood" are all calculated server-side.
                Calculated = [JsonPointer.Parse("/properties/fortune"),
                              JsonPointer.Parse("/properties/answeredAt"),
                              JsonPointer.Parse("/properties/confidence"),
                              JsonPointer.Parse("/properties/mood")],
                // These fields are read-only — the user can't set them.
                ReadOnly = [JsonPointer.Parse("/properties/fortune"),
                            JsonPointer.Parse("/properties/confidence"),
                            JsonPointer.Parse("/properties/mood")],
                // Echo back any unevaluated paths from the request.
                Unevaluated = request.Metadata?.Unevaluated,
            },
        };

        return Task.FromResult<OneOf<TypedResourcePreview, ErrorResponse>>(preview);
    }
}
