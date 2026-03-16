// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts.Exceptions;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;

namespace Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;

/// <summary>
/// Defines a handler for previewing a resource deployment.
/// </summary>
public interface IResourcePreviewHandler : IHandler<ResourcePreviewSpecification, OneOf<ResourcePreview, ErrorResponse>>
{
}
