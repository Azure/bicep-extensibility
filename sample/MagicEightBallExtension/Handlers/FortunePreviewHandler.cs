// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Handlers;
using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json.Nodes;

namespace MagicEightBallExtension.Handlers;

/// <summary>
/// Previews what a fortune resource would look like without persisting it.
/// Demonstrates handling of unevaluated expressions and preview metadata.
/// </summary>
[SupportedExtensionVersionRange(">=1.0.0")]
public class FortunePreviewHandler : TypedResourcePreviewHandler
{
    public FortunePreviewHandler(IHttpContextAccessor httpContextAccessor)
        : base(httpContextAccessor)
    {
    }

    protected override Task<OneOf<ResourcePreview, ErrorResponse>> PreviewResourceAsync(
        ResourcePreviewSpecification specification, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Previewing resource of type '{Type}' with apiVersion '{ApiVersion}'.", specification.Type, specification.ApiVersion);

        var properties = specification.Properties.DeepClone().AsObject();

        // Check for unevaluated expressions — these are ARM template expressions that
        // couldn't be resolved at preview time. We echo them back as-is per the contract.
        var unevaluatedPaths = specification.Metadata?.Unevaluated;

        // If the "question" property is evaluable, generate a preview fortune.
        if (properties["question"] is JsonValue questionValue &&
            questionValue.TryGetValue<string>(out _))
        {
            properties["fortune"] = "Preview: The stars are aligning... (actual fortune generated on create)";
            properties["answeredAt"] = DateTimeOffset.UtcNow.ToString("o");
        }

        var identifiers = new JsonObject { ["name"] = properties["name"]?.DeepClone() ?? "preview" };

        var preview = new ResourcePreview
        {
            Type = specification.Type,
            ApiVersion = specification.ApiVersion,
            Identifiers = identifiers,
            Properties = properties,
            Config = specification.Config?.DeepClone()?.AsObject(),
            ConfigId = specification.Config is not null ? "static-config-id" : null,
            Metadata = new ResourcePreviewMetadata
            {
                // "fortune" and "answeredAt" are calculated server-side.
                Calculated = [Json.Pointer.JsonPointer.Parse("/properties/fortune"),
                              Json.Pointer.JsonPointer.Parse("/properties/answeredAt")],
                // "fortune" is read-only — the user can't set it.
                ReadOnly = [Json.Pointer.JsonPointer.Parse("/properties/fortune")],
                // Echo back any unevaluated paths from the request.
                Unevaluated = unevaluatedPaths,
            },
        };

        return Task.FromResult<OneOf<ResourcePreview, ErrorResponse>>(preview);
    }
}
