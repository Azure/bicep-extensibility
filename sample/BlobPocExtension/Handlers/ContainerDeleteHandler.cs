// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Handlers;
using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using BlobPocExtension.Storage;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace BlobPocExtension.Handlers;

public sealed class ContainerDeleteHandler : TypedResourceDeleteHandler<ContainerProperties, ContainerIdentifiers>
{
    private readonly IStorageClientFactory factory;

    public ContainerDeleteHandler(IOptions<JsonOptions> jsonOptions, IStorageClientFactory factory)
        : base(jsonOptions) => this.factory = factory;

    protected override async Task<OneOf<TypedResource?, LongRunningOperation, ErrorResponse>> HandleAsync(
        TypedResourceReference request, CancellationToken cancellationToken)
    {
        // Account-name validation and RequestFailedException mapping run in the pipeline (behaviors).
        var container = this.factory.GetContainerClient(request.Identifiers.AccountName, request.Identifiers.ContainerName);

        // Idempotent: deleting a missing container is still success.
        await container.DeleteIfExistsAsync(cancellationToken: cancellationToken);

        // Null signals 204 No Content (resource deleted or never existed).
        return (TypedResource?)null;
    }
}
