// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Models;
using Azure.Deployments.Extensibility.Core.V2.Validation;
using Azure.Deployments.Extensibility.Extensions.Kubernetes.Validation.Schemas;

namespace Azure.Deployments.Extensibility.Extensions.Kubernetes.Validation
{
    internal sealed class K8sResourceReferenceValidator : ModelValidator<ResourceReference>, IK8sResourceReferenceValidator
    {
        public K8sResourceReferenceValidator()
        {
            this.AnyValid(x => x.Type)
                .MustMatchRegex(RegexPatterns.ResourceType())
                    .WithErrorCode("InvalidResourceType");

            this.AnyValid(x => x.ApiVersion)
                .MustNotBeNull()
                    .WithErrorCode("NullApiVersion")
                    .WithErrorMessage("The Kubernetes resource API version must be provided and cannot be null.").AndThen
                .MustMatchRegex(RegexPatterns.ApiVersion())
                    .WithErrorCode("InvalidApiVersion");

            this.WhenPrecedingRulesSatisfied(x => x.Identifiers)
                .MustMatchJsonSchema(JsonSchemas.K8sResourceIdentifiers)
                    .WithErrorCode("InvalidIdentifier");

            this.AnyValid(x => x.Config)
                .MustNotBeNull()
                    .WithErrorCode("NullConfig")
                    .WithErrorMessage("The Kubernetes config must be provided and cannot be null.").AndThen
                .MustMatchJsonSchema(JsonSchemas.K8sResourceConfig)
                    .WithErrorCode("InvalidConfig");
        }
    }
}
