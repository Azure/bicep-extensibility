// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Models;
using Azure.Deployments.Extensibility.Core.V2.Validation;

namespace Azure.Deployments.Extensibility.Extensions.Kubernetes.Validation
{
    public interface IK8sResourceReferenceValidator : IModelValidator<ResourceReference>
    {
    }
}
