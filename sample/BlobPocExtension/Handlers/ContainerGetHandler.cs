// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Handlers;
using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using BlobPocExtension.Storage;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace BlobPocExtension.Handlers;

public sealed class ContainerGetHandler : TypedResourceGetHandler<ContainerProperties, ContainerIdentifiers>
{
    private readonly IStorageClientFactory factory;

    public ContainerGetHandler(IOptions<JsonOptions> jsonOptions, IStorageClientFactory factory)
        : base(jsonOptions) => this.factory = factory;

    protected override async Task<OneOf<TypedResource?, ErrorResponse>> HandleAsync(
        TypedResourceReference request, CancellationToken cancellationToken)
    {
        // Account-name validation and RequestFailedException mapping run in the pipeline (behaviors).
        var container = this.factory.GetContainerClient(request.Identifiers.AccountName, request.Identifiers.ContainerName);

        var exists = await container.ExistsAsync(cancellationToken);
        if (!exists.Value)
        {
            // Null signals 404 to the engine (the resource does not exist).
            return (TypedResource?)null;
        }

        var properties = await container.GetPropertiesAsync(cancellationToken: cancellationToken);

        return new TypedResource
        {
            Type = request.Type,
            ApiVersion = request.ApiVersion,
            Identifiers = request.Identifiers,
            Properties = new ContainerProperties
            {
                AccountName = request.Identifiers.AccountName,
                ContainerName = request.Identifiers.ContainerName,
                PublicAccess = ContainerAccess.FromPublicAccessType(properties.Value.PublicAccess),
            },
        };
    }
}
