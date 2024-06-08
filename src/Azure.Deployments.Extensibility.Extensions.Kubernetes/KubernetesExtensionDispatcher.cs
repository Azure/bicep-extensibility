// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore;
using Azure.Deployments.Extensibility.AspNetCore.Exceptions;
using Azure.Deployments.Extensibility.Core.V2.Models;
using Azure.Deployments.Extensibility.Core.V2.Validation;
using Azure.Deployments.Extensibility.Extensions.Kubernetes.Client;
using Semver;

namespace Azure.Deployments.Extensibility.Extensions.Kubernetes
{
    internal class KubernetesExtensionDispatcher : IExtensionDispatcher
    {
        private readonly IModelValidator<ResourceSpecification> resourceSpecificationValidator;
        private readonly IModelValidator<ResourceReference> resourceReferenceValidator;
        private readonly IK8sClientFactory clientFactory;

        public KubernetesExtensionDispatcher(
            IModelValidator<ResourceSpecification> resourceSpecificationValidator,
            IModelValidator<ResourceReference> resourceReferenceValidator,
            IK8sClientFactory clientFactory)
        {
            this.resourceSpecificationValidator = resourceSpecificationValidator;
            this.resourceReferenceValidator = resourceReferenceValidator;
            this.clientFactory = clientFactory;
        }

        public IExtension DispatchExtension(string extensionVersion)
        {
            ValidateExtensionVersion(extensionVersion);

            return new KubernetesExtension(
                this.resourceSpecificationValidator,
                this.resourceReferenceValidator,
                this.clientFactory);
        }

        private static void ValidateExtensionVersion(string extensionVersion)
        {
            if (!SemVersion.TryParse(extensionVersion, SemVersionStyles.Strict, out var _))
            {
                throw new ErrorResponseException("InvalidKubernetesExtensionVersion", "The extension version must be in the Semantic Versioning 2.0 format.");
            }
        }
    }
}
