// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Handlers;
using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace BlobPocExtension.Handlers;

public sealed class BlobCreateOrUpdateHandler : TypedResourceCreateOrUpdateHandler<BlobProperties, BlobIdentifiers>
{
    public BlobCreateOrUpdateHandler(IOptions<JsonOptions> jsonOptions) : base(jsonOptions) { }

    protected override Task<OneOf<TypedResource, LongRunningOperation, ErrorResponse>> HandleAsync(
        TypedResourceSpecification request, CancellationToken cancellationToken)
    {
        // TODO: replace with a real BlobClient .UploadAsync() call.
        var resource = new TypedResource
        {
            Type = request.Type,
            ApiVersion = request.ApiVersion,
            Identifiers = new BlobIdentifiers
            {
                AccountName = request.Properties.AccountName,
                ContainerName = request.Properties.ContainerName,
                BlobName = request.Properties.BlobName,
            },
            Properties = request.Properties,
        };

        return Task.FromResult<OneOf<TypedResource, LongRunningOperation, ErrorResponse>>(resource);
    }
}
