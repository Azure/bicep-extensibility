// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;

namespace Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;

/// <summary>
/// Defines a handler for creating or updating a resource.
/// </summary>
public interface IResourceCreateOrUpdateHandler : IHandler<ResourceSpecification, OneOf<Resource, LongRunningOperation, ErrorResponse>>
{
}
