﻿using Azure.Deployments.Extensibility.Core.Json;
using Azure.Deployments.Extensibility.Providers.Kubernetes.Models;
using k8s.Models;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.Extensions
{
    public static class KubernetesResourcePropertiesExtensions
    {
        public static V1Patch ToV1Patch(this KubernetesResourceProperties properties)
        {
            var propertiesContent = JsonSerializers.CamelCase.Serialize(properties);


            return new(propertiesContent, V1Patch.PatchType.ApplyPatch);
        }
    }
}
