// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using k8s;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Models
{
    public record K8sClusterAccessConfig
    {
        private K8sClusterAccessConfig(KubernetesClientConfiguration clientConfiguration, string? @namespace)
        {
            this.ClientConfiguration = clientConfiguration;
            this.Namespace = @namespace;
        }

        public KubernetesClientConfiguration ClientConfiguration { get; }

        public string? Namespace { get; }

        public static async Task<K8sClusterAccessConfig> FromAsync(JsonObject? configObject)
        {
            ArgumentNullException.ThrowIfNull(configObject);

            var kubeConfig = configObject["kubeConfig"]?.GetValue<string>();

            ArgumentNullException.ThrowIfNull(kubeConfig);

            var kubeConfigBytes = Convert.FromBase64String(kubeConfig);
            var context = configObject["context"]?.GetValue<string>();
            var @namespace = configObject["namespace"]?.GetValue<string>();

            var clientConfiguration = await KubernetesClientConfiguration.BuildConfigFromConfigFileAsync(
                new MemoryStream(kubeConfigBytes),
                currentContext: context);

            return new(clientConfiguration, @namespace ?? clientConfiguration.Namespace);
        }
    }
}
