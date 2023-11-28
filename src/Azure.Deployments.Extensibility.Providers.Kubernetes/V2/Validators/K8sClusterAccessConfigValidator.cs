// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Models;
using Azure.Deployments.Extensibility.Core.V2.Validators;
using Azure.Deployments.Extensibility.Providers.Kubernetes.Extensions;
using Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Schemas;
using Json.Pointer;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Validators
{
    public sealed class K8sClusterAccessConfigValidator : ResourceConfigSchemaValidator
    {
        public K8sClusterAccessConfigValidator()
            : base(JsonSchemas.K8sClusterAccessConfig)
        {
        }

        public override IReadOnlyList<ErrorDetail> Validate(JsonObject? config)
        {
            if (base.Validate(config) is { Count: > 0 } schemaErrors)
            {
                return schemaErrors;
            }

            if (config is not null &&
                config["kubeConfig"] is JsonValue kubeConfigValue &&
                !kubeConfigValue.GetValue<string>().IsBase64Encoded())
            {
                return new[]
                {
                    new ErrorDetail("InvalidKubeConfig", "Value must be a Base64-encoded string.", JsonPointer.Create("config", "kubeConfig")),
                };
            }

            return Array.Empty<ErrorDetail>();
        }
    }
}
