// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore;
using Azure.Deployments.Extensibility.AspNetCore.Exceptions;
using Azure.Deployments.Extensibility.Extensions.Kubernetes.Client;
using Azure.Deployments.Extensibility.Extensions.Kubernetes.Validation;
using Semver;

namespace Azure.Deployments.Extensibility.Extensions.Kubernetes
{
    public class KubernetesExtensionDispatcher : IExtensionDispatcher
    {
        private readonly IK8sResourceSpecificationValidator resourceSpecificationValidator;
        private readonly IK8sResourceReferenceValidator resourceReferenceValidator;
        private readonly IK8sClientFactory clientFactory;

        public KubernetesExtensionDispatcher(
            IK8sResourceSpecificationValidator resourceSpecificationValidator,
            IK8sResourceReferenceValidator resourceReferenceValidator,
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
