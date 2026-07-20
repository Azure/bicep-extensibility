// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Handlers;
using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using BlobPocExtension.Storage;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace BlobPocExtension.Handlers;

public sealed class ContainerCreateOrUpdateHandler : TypedResourceCreateOrUpdateHandler<ContainerProperties, ContainerIdentifiers>
{
    private readonly IStorageClientFactory factory;

    public ContainerCreateOrUpdateHandler(IOptions<JsonOptions> jsonOptions, IStorageClientFactory factory)
        : base(jsonOptions) => this.factory = factory;

    protected override async Task<OneOf<TypedResource, LongRunningOperation, ErrorResponse>> HandleAsync(
        TypedResourceSpecification request, CancellationToken cancellationToken)
    {
        // Account-name validation (AccountNameValidationBehavior) and RequestFailedException mapping
        // (StorageExceptionHandlingBehavior) run in the pipeline, so this handler does pure I/O.
        var container = this.factory.GetContainerClient(request.Properties.AccountName, request.Properties.ContainerName);

        await container.CreateIfNotExistsAsync(
            ContainerAccess.ToPublicAccessType(request.Properties.PublicAccess),
            cancellationToken: cancellationToken);

        return new TypedResource
        {
            Type = request.Type,
            ApiVersion = request.ApiVersion,
            Identifiers = new ContainerIdentifiers
            {
                AccountName = request.Properties.AccountName,
                ContainerName = request.Properties.ContainerName,
            },
            Properties = request.Properties with { PublicAccess = request.Properties.PublicAccess ?? "none" },
        };
    }
}
