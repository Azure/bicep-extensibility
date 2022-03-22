using System;
using System.Text.Json.Serialization;

namespace Extensibility.Kubernetes
{
    public class KubernetesConfig
    {
        [JsonPropertyName("namespace")]
        public string? Namespace { get; set; }

        // Will be serialized as base64-encoded JSON string.
        [JsonPropertyName("kubeConfig")]
        public byte[] KubeConfig { get; set; } = Array.Empty<byte>();

        [JsonPropertyName("context")]
        public string? Context { get; set; }
    }
}
