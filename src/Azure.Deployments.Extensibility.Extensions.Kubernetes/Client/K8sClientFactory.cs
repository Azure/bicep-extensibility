// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Exceptions;
using Json.Pointer;
using k8s;
using k8s.Exceptions;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Extensions.Kubernetes.Client
{
    internal class K8sClientFactory : IK8sClientFactory
    {
        public async Task<IK8sClient> CreateAsync(JsonObject? config)
        {
            ArgumentNullException.ThrowIfNull(config, nameof(config));

            try
            {
                var kubeConfig = config["kubeConfig"]?.GetValue<string>() ?? throw new InvalidOperationException("Expected kubeConfig to be non-null.");
                var kubeConfigBytes = Convert.FromBase64String(kubeConfig);
                var kubeConfigStream = new MemoryStream(kubeConfigBytes);
                var currentContext = config["context"]?.GetValue<string>();
                var clientConfiguration = await KubernetesClientConfiguration.BuildConfigFromConfigFileAsync(kubeConfigStream, currentContext: currentContext);

                if (config["namespace"]?.GetValue<string>() is { } namespaceOverride)
                {
                    clientConfiguration.Namespace = namespaceOverride;
                }

                var kubernetes = new k8s.Kubernetes(clientConfiguration);

                return new K8sClient(clientConfiguration);
            }
            catch (KubeConfigException exception)
            {
                throw new ErrorResponseException("InvalidKubeConfig", exception.Message, JsonPointer.Create("config", "kubeConfig"));
            }
        }
    }
}
