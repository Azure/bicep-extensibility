// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Security.Cryptography;
using System.Text;

namespace Azure.Deployments.Extensibility.Extensions.Kubernetes.Models
{
    internal record K8sObjectIdentifiers(string Name, string? Namespace)
    {
        public static K8sObjectIdentifiers Create(K8sObject k8sObject) => new(k8sObject.Name, k8sObject.Namespace);

        public static string CalculateServerHostHash(string serverOrigin) => Hash(Encoding.UTF8.GetBytes(serverOrigin));

        private static string Hash(byte[] data) => Convert.ToHexString(SHA256.HashData(data));
    }
}
