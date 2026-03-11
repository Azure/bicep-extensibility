// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using Azure.Deployments.Extensibility.Core.V2.Validation;
using Azure.Deployments.Extensibility.Extensions.Kubernetes.Validation.Schemas;

namespace Azure.Deployments.Extensibility.Extensions.Kubernetes.Validation
{
    internal sealed class K8sResourceReferenceValidator : ModelValidator<ResourceReference>, IK8sResourceReferenceValidator
    {
        public K8sResourceReferenceValidator()
        {
            var typeRule = this.Ensure(x => x.Type)
                .MatchesRegex(RegexPatterns.ResourceType(), error => error
                    .WithCode("InvalidResourceType"));

            var apiVersionRule = this.Ensure(x => x.ApiVersion)
                .NotNull(error => error
                    .WithCode("NullApiVersion")
                    .WithMessage("The Kubernetes resource API version must be provided and cannot be null."))
                .MatchesRegex(RegexPatterns.ApiVersion(), error => error
                    .WithCode("InvalidApiVersion"));

            this.Ensure(x => x.Identifiers)
                .MatchesJsonSchema(JsonSchemas.K8sResourceIdentifiers, configureError: error => error
                    .WithCode("InvalidIdentifier"))
                .DependsOn(typeRule, apiVersionRule);

            this.Ensure(x => x.Config)
                .NotNull(error => error
                    .WithCode("NullConfig")
                    .WithMessage("The Kubernetes config must be provided and cannot be null."))
                .MatchesJsonSchema(JsonSchemas.K8sResourceConfig, configureError: error => error
                    .WithCode("InvalidConfig"));
        }
    }
}
