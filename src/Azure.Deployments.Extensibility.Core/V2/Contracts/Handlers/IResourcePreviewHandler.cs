// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts.Exceptions;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;

namespace Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;

/// <summary>
/// Defines a handler for previewing a resource deployment.
/// Simulates a create or update without persisting changes, used for preflight validation and What-If.
/// </summary>
/// <remarks>
/// See <see href="https://github.com/Azure/bicep-extensibility/blob/main/docs/v2/contract.md#preview-resource">contract.md – Preview Resource</see>
/// and <see href="https://github.com/Azure/bicep-extensibility/blob/main/docs/v2/preview-operation.md">preview-operation.md</see>.
/// </remarks>
public interface IResourcePreviewHandler : IHandler<ResourcePreviewSpecification, OneOf<ResourcePreview, ErrorResponse>>
{
}
