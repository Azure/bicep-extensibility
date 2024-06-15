// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Json;
using Azure.Deployments.Extensibility.Core.V2.Models;
using Azure.Deployments.Extensibility.Extensions.Kubernetes.Models;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Extensions.Kubernetes.Tests.Integration.TestFixtures
{
    public record K8sResourceSpecification : ResourceSpecification
    {
        private readonly static string Base64EncodedKubeConfig = LoadContentAsBase64EncodedString();

        [SetsRequiredMembers]
        public K8sResourceSpecification(string type, string apiVersion, string propertiesJson)
            : base(
                  type,
                  apiVersion,
                  JsonNode.Parse(propertiesJson)?.AsObject() ?? throw new ArgumentException("Argument is not a valid JSON object.", nameof(propertiesJson)),
                  new JsonObject
                  {
                    ["kubeConfig"] = Base64EncodedKubeConfig,
                  })
        {
        }

        public string Name => this.Properties.GetPropertyValue<string>("/metadata/name");

        public string? NamespaceInMetadata => this.Properties.TryGetPropertyValue<string>("/metadata/namespace");

        public override string ToString() => $"Type={this.Type}";

        private static string LoadContentAsBase64EncodedString()
        {
            var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var kubeConfigPath = Path.Combine(homeDirectory, ".kube", "config");
            var bytes = File.ReadAllBytes(kubeConfigPath);

            return Convert.ToBase64String(bytes);
        }
    }
}
