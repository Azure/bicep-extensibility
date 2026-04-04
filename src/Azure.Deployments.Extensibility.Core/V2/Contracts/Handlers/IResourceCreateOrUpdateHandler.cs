// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;

namespace Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;

/// <summary>
/// Defines a handler for creating or updating a resource.
/// The operation may complete synchronously or initiate a long-running operation.
/// </summary>
/// <remarks>
/// See <see href="https://github.com/Azure/bicep-extensibility/blob/main/docs/v2/contract.md#create-or-update-resource">contract.md – Create or Update Resource</see>.
/// </remarks>
public interface IResourceCreateOrUpdateHandler : IHandler<ResourceSpecification, OneOf<Resource, LongRunningOperation, ErrorResponse>>
{
}
