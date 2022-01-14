using System;
using Newtonsoft.Json;

namespace Extensibility.Kubernetes
{
    public class KubernetesConfig
    {
        [JsonProperty("namespace")]
        public string? Namespace { get; set; }

        // Will be serialized as base64-encoded JSON string.
        [JsonProperty("kubeConfig")]
        public byte[] KubeConfig { get; set; } = Array.Empty<byte>();

        [JsonProperty("context")]
        public string? Context { get; set; }
    }
}