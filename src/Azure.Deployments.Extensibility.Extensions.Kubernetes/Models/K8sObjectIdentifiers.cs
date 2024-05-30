// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Extensions.Kubernetes.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Extensions.Kubernetes.Models
{
    public record K8sObjectIdentifiers(string Name, string? Namespace, string? ServerHostHash)
    {
        public static K8sObjectIdentifiers Create(K8sObject k8sObject, string serverHost)
        {
            var serverHostHash = CalculateServerHostHash(serverHost, k8sObject.Name);

            return new(k8sObject.Name, k8sObject.Namespace, serverHostHash);
        }

        public static K8sObjectIdentifiers From(JsonObject identifiersObject) =>
            JsonSerializer.Deserialize(identifiersObject, K8sModelSerializerContext.WithDefaultOptions.K8sObjectIdentifiers) ??
            throw new InvalidOperationException("Could not deserialize the identifiers object.");

        public bool MatchesServerHost(string serverHost)
        {
            var serverHostHash = CalculateServerHostHash(serverHost, this.Name);

            return string.Equals(this.ServerHostHash, serverHostHash, StringComparison.Ordinal);
        }

        private static string CalculateServerHostHash(string serverOrigin, string name)
        {
            var serverOriginBytes = Encoding.UTF8.GetBytes(serverOrigin);
            var nameBytes = Encoding.UTF8.GetBytes(name);

            return Hash(serverOriginBytes, nameBytes);
        }

        private static string Hash(byte[] data, byte[] salt)
        {
            var saltedData = new byte[data.Length + salt.Length];
            data.CopyTo(saltedData, 0);
            salt.CopyTo(saltedData, data.Length);

            return Convert.ToHexString(SHA256.HashData(saltedData));
        }
    }
    
}
