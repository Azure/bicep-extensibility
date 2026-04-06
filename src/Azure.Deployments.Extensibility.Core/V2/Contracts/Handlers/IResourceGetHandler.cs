// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;

namespace Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;

/// <summary>
/// Defines a handler for retrieving the current state of a resource.
/// Return a non-null <see cref="Resource"/> if the resource exists, or <see langword="null"/> if it does not.
/// Return an <see cref="ErrorResponse"/> if the <see cref="ResourceReference"/> is invalid or another error occurs.
/// </summary>
/// <remarks>
/// See <see href="https://github.com/Azure/bicep-extensibility/blob/main/docs/v2/contract.md#get-resource">contract.md – Get Resource</see>.
/// </remarks>
public interface IResourceGetHandler : IHandler<ResourceReference, OneOf<Resource?, ErrorResponse>>
{
}
