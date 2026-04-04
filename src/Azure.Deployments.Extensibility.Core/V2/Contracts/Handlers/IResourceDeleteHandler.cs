// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;

namespace Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;


/// <summary>
/// Defines a handler for deleting a resource.
/// The operation may complete synchronously or initiate a long-running operation.
/// </summary>
/// <remarks>
/// See <see href="https://github.com/Azure/bicep-extensibility/blob/main/docs/v2/contract.md#delete-resource">contract.md – Delete Resource</see>.
/// </remarks>
public interface IResourceDeleteHandler : IHandler<ResourceReference, OneOf<Resource?, LongRunningOperation, ErrorResponse>>
{
}
