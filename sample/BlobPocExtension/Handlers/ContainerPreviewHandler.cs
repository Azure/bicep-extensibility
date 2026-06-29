// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Handlers;
using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using Json.Pointer;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace BlobPocExtension.Handlers;

public sealed class ContainerPreviewHandler : TypedResourcePreviewHandler<ContainerProperties, ContainerIdentifiers>
{
    public ContainerPreviewHandler(IOptions<JsonOptions> jsonOptions) : base(jsonOptions) { }

    protected override Task<OneOf<TypedResourcePreview, ErrorResponse>> HandleAsync(
        TypedResourcePreviewSpecification request, CancellationToken cancellationToken)
    {
        // Account-name validation runs in the pipeline (AccountNameValidationBehavior).
        // Side-effect-free: echo, and default publicAccess to "none".
        var properties = request.Properties;
        if (properties.PublicAccess is null)
        {
            properties = properties with { PublicAccess = "none" };
        }

        var preview = new TypedResourcePreview
        {
            Type = request.Type,
            ApiVersion = request.ApiVersion,
            Identifiers = new ContainerIdentifiers
            {
                AccountName = properties.AccountName,
                ContainerName = properties.ContainerName,
            },
            Properties = properties,
            Metadata = new ResourcePreviewMetadata
            {
                // "publicAccess" is defaulted server-side when not provided.
                Calculated = [JsonPointer.Parse("/properties/publicAccess")],
                Unevaluated = request.Metadata?.Unevaluated,
            },
        };

        return Task.FromResult<OneOf<TypedResourcePreview, ErrorResponse>>(preview);
    }
}
