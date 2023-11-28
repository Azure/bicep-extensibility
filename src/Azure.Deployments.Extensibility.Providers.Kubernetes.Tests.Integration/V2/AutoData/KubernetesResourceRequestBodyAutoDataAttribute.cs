// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AutoFixture;
using AutoFixture.Xunit2;
using Azure.Deployments.Extensibility.Core.V2.Models;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Integration.V2.AutoData
{
    public class KubernetesResourceRequestBodyAutoDataAttribute(string resourceType, string propertiesJson, string? @namespace = null)
        : AutoDataAttribute(() => CreateFixture(resourceType, propertiesJson, @namespace))
    {
        private static readonly string KubeConfig = LoadKubeConfig();

        private static Fixture CreateFixture(string resourceType, string propertiesJson, string? @namespace)
        {
            var fixture = new Fixture();

            var properties = JsonNode.Parse(propertiesJson)?.AsObject() ?? throw new ArgumentException("Invalid JSON.", nameof(propertiesJson));
            var config = new JsonObject
            {
                ["kubeConfig"] = KubeConfig,
                ["namespace"] = @namespace,
            };

            fixture.Inject(new ResourceRequestBody(resourceType, properties, config));

            return fixture;
        }

        private static string LoadKubeConfig()
        {
            var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var kubeConfigPath = Path.Combine(homeDirectory, ".kube", "config");
            var bytes = File.ReadAllBytes(kubeConfigPath);

            return Convert.ToBase64String(bytes);
        }
    }
}
