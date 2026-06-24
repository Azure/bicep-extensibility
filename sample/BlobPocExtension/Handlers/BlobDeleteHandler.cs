// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Handlers;
using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace BlobPocExtension.Handlers;

public sealed class BlobDeleteHandler : TypedResourceDeleteHandler<BlobProperties, BlobIdentifiers>
{
    public BlobDeleteHandler(IOptions<JsonOptions> jsonOptions) : base(jsonOptions) { }

    protected override Task<OneOf<TypedResource?, LongRunningOperation, ErrorResponse>> HandleAsync(
        TypedResourceReference request, CancellationToken cancellationToken)
    {
        // TODO: replace with a real BlobClient .DeleteIfExistsAsync() call.
        // Returning null signals 204 No Content (resource deleted or never existed).
        TypedResource? result = null;

        return Task.FromResult<OneOf<TypedResource?, LongRunningOperation, ErrorResponse>>(result);
    }
}
