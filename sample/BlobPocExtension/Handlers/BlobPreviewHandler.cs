// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Handlers;
using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using Json.Pointer;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace BlobPocExtension.Handlers;

public sealed class BlobPreviewHandler : TypedResourcePreviewHandler<BlobProperties, BlobIdentifiers>
{
    public BlobPreviewHandler(IOptions<JsonOptions> jsonOptions) : base(jsonOptions) { }

    protected override Task<OneOf<TypedResourcePreview, ErrorResponse>> HandleAsync(
        TypedResourcePreviewSpecification request, CancellationToken cancellationToken)
    {
        var properties = request.Properties;

        // TODO: replace with a real BlobClient existence/content lookup.
        // Default the content type when not supplied so the preview reflects the persisted shape.
        if (properties.ContentType is null)
        {
            properties = properties with { ContentType = "application/octet-stream" };
        }

        var preview = new TypedResourcePreview
        {
            Type = request.Type,
            ApiVersion = request.ApiVersion,
            Identifiers = new BlobIdentifiers
            {
                AccountName = properties.AccountName,
                ContainerName = properties.ContainerName,
                BlobName = properties.BlobName,
            },
            Properties = properties,
            Metadata = new ResourcePreviewMetadata
            {
                // "contentType" is defaulted server-side when not provided.
                Calculated = [JsonPointer.Parse("/properties/contentType")],
                // Echo back any unevaluated paths from the request.
                Unevaluated = request.Metadata?.Unevaluated,
            },
        };

        return Task.FromResult<OneOf<TypedResourcePreview, ErrorResponse>>(preview);
    }
}
