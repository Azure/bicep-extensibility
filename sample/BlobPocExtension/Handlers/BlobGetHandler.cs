// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Handlers;
using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace BlobPocExtension.Handlers;

public sealed class BlobGetHandler : TypedResourceGetHandler<BlobProperties, BlobIdentifiers>
{
    public BlobGetHandler(IOptions<JsonOptions> jsonOptions) : base(jsonOptions) { }

    protected override Task<OneOf<TypedResource?, ErrorResponse>> HandleAsync(
        TypedResourceReference request, CancellationToken cancellationToken)
    {
        // TODO: replace with a real BlobClient .DownloadContentAsync() call.
        var resource = new TypedResource
        {
            Type = request.Type,
            ApiVersion = request.ApiVersion,
            Identifiers = request.Identifiers,
            Properties = new BlobProperties
            {
                AccountName = request.Identifiers.AccountName,
                ContainerName = request.Identifiers.ContainerName,
                BlobName = request.Identifiers.BlobName,
                Content = "hello from the POC",
                ContentType = "text/plain",
            },
        };

        return Task.FromResult<OneOf<TypedResource?, ErrorResponse>>(resource);
    }
}
