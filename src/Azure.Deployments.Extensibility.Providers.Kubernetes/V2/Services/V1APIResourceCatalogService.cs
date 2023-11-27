// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Exceptions;
using Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Models;
using Json.Pointer;
using k8s;
using k8s.Autorest;
using k8s.Models;
using System.Collections.ObjectModel;
using System.Net;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Services
{
    public class V1APIResourceCatalogService(IKubernetes kubernetes) : IV1APIResourceCatalogService
    {
        private readonly static ReadOnlyDictionary<string, ReadOnlyDictionary<K8sResourceType, V1APIResource>> V1APIResourceCatalogIndex = CreateV1APIResourceCatalogIndex();

        public async Task<V1APIResource> FindV1APIResourceAsync(string providerVersion, K8sResourceType resourceType, CancellationToken cancellationToken)
        {
            var majorMinorVersion = ExtractMajorMinorVersion(providerVersion);

            if (V1APIResourceCatalogIndex.TryGetValue(majorMinorVersion, out var catalog) &&
                catalog.TryGetValue(resourceType, out var matchingResource))
            {
                return matchingResource;
            }

            var (group, version, kind) = resourceType;
            var client = new GenericClient(kubernetes, group, version, plural: "", disposeClient: false);

            try
            {
                var apiResourceList = await client.ListAsync<V1APIResourceList>(cancellationToken);

                return apiResourceList.Resources.SingleOrDefault(x => x.Kind.Equals(kind, StringComparison.Ordinal))
                    ?? throw UnknownResourceTypeException(resourceType);
            }
            catch (HttpOperationException exception) when (exception.Response.StatusCode == HttpStatusCode.NotFound)
            {
                throw UnknownResourceTypeException(resourceType);
            }
        }

        private static ReadOnlyDictionary<string, ReadOnlyDictionary<K8sResourceType, V1APIResource>> CreateV1APIResourceCatalogIndex()
        {
            var catalogIndex = new Dictionary<string, ReadOnlyDictionary<K8sResourceType, V1APIResource>>
            {
                // TODO: Add data for other MAJOR.MINOR releases.
                ["1.28"] = CreateV1APIResourceCatalog(
                    ("bindings", "v1", true, "Binding"),
                    ("componentstatuses", "v1", false, "ComponentStatus"),
                    ("configmaps", "v1", true, "ConfigMap"),
                    ("endpoints", "v1", true, "Endpoints"),
                    ("events", "v1", true, "Event"),
                    ("limitranges", "v1", true, "LimitRange"),
                    ("namespaces", "v1", false, "Namespace"),
                    ("nodes", "v1", false, "Node"),
                    ("persistentvolumeclaims", "v1", true, "PersistentVolumeClaim"),
                    ("persistentvolumes", "v1", false, "PersistentVolume"),
                    ("pods", "v1", true, "Pod"),
                    ("podtemplates", "v1", true, "PodTemplate"),
                    ("replicationcontrollers", "v1", true, "ReplicationController"),
                    ("resourcequotas", "v1", true, "ResourceQuota"),
                    ("secrets", "v1", true, "Secret"),
                    ("serviceaccounts", "v1", true, "ServiceAccount"),
                    ("services", "v1", true, "Service"),
                    ("mutatingwebhookconfigurations", "admissionregistration.k8s.io/v1", false, "MutatingWebhookConfigurati"),
                    ("validatingwebhookconfigurations", "admissionregistration.k8s.io/v1", false, "ValidatingWebhookConfigura"),
                    ("customresourcedefinitions", "apiextensions.k8s.io/v1", false, "CustomResourceDefinition"),
                    ("apiservices", "apiregistration.k8s.io/v1", false, "APIService"),
                    ("controllerrevisions", "apps/v1", true, "ControllerRevision"),
                    ("daemonsets", "apps/v1", true, "DaemonSet"),
                    ("deployments", "apps/v1", true, "Deployment"),
                    ("replicasets", "apps/v1", true, "ReplicaSet"),
                    ("statefulsets", "apps/v1", true, "StatefulSet"),
                    ("selfsubjectreviews", "authentication.k8s.io/v1", false, "SelfSubjectReview"),
                    ("tokenreviews", "authentication.k8s.io/v1", false, "TokenReview"),
                    ("localsubjectaccessreviews", "authorization.k8s.io/v1", true, "LocalSubjectAccessReview"),
                    ("selfsubjectaccessreviews", "authorization.k8s.io/v1", false, "SelfSubjectAccessReview"),
                    ("selfsubjectrulesreviews", "authorization.k8s.io/v1", false, "SelfSubjectRulesReview"),
                    ("subjectaccessreviews", "authorization.k8s.io/v1", false, "SubjectAccessReview"),
                    ("horizontalpodautoscalers", "autoscaling/v2", true, "HorizontalPodAutoscaler"),
                    ("cronjobs", "batch/v1", true, "CronJob"),
                    ("jobs", "batch/v1", true, "Job"),
                    ("certificatesigningrequests", "certificates.k8s.io/v1", false, "CertificateSigningRequest"),
                    ("leases", "coordination.k8s.io/v1", true, "Lease"),
                    ("endpointslices", "discovery.k8s.io/v1", true, "EndpointSlice"),
                    ("events", "events.k8s.io/v1", true, "Event"),
                    ("flowschemas", "flowcontrol.apiserver.k8s.io/v1beta3", false, "FlowSchema"),
                    ("prioritylevelconfigurations", "flowcontrol.apiserver.k8s.io/v1beta3", false, "PriorityLevelConfiguration"),
                    ("ingressclasses", "networking.k8s.io/v1", false, "IngressClass"),
                    ("ingresses", "networking.k8s.io/v1", true, "Ingress"),
                    ("networkpolicies", "networking.k8s.io/v1", true, "NetworkPolicy"),
                    ("runtimeclasses", "node.k8s.io/v1", false, "RuntimeClass"),
                    ("poddisruptionbudgets", "policy/v1", true, "PodDisruptionBudget"),
                    ("clusterrolebindings", "rbac.authorization.k8s.io/v1", false, "ClusterRoleBinding"),
                    ("clusterroles", "rbac.authorization.k8s.io/v1", false, "ClusterRole"),
                    ("rolebindings", "rbac.authorization.k8s.io/v1", true, "RoleBinding"),
                    ("roles", "rbac.authorization.k8s.io/v1", true, "Role"),
                    ("priorityclasses", "scheduling.k8s.io/v1", false, "PriorityClass"),
                    ("csidrivers", "storage.k8s.io/v1", false, "CSIDriver"),
                    ("csinodes", "storage.k8s.io/v1", false, "CSINode"),
                    ("csistoragecapacities", "storage.k8s.io/v1", true, "CSIStorageCapacity"),
                    ("storageclasses", "storage.k8s.io/v1", false, "StorageClass"),
                    ("volumeattachments", "storage.k8s.io/v1", false, "VolumeAttachment"))
            };

            return catalogIndex.AsReadOnly();
        }

        private static ReadOnlyDictionary<K8sResourceType, V1APIResource> CreateV1APIResourceCatalog(params (string Name, string ApiVersion, bool Namespaced, string Kind)[] apiResourceEntry) => apiResourceEntry
            .Select(x => CreateV1APIResource(x.Name, x.ApiVersion, x.Namespaced, x.Kind))
            .ToDictionary(x => new K8sResourceType(x.Group, x.Version, x.Kind))
            .AsReadOnly();

        private static V1APIResource CreateV1APIResource(string name, string apiVersion, bool namespaced, string kind)
        {
            var apiVersionParts = apiVersion.Split('/');
            var group = apiVersionParts.Length > 1 ? apiVersionParts[0] : "";
            var version = apiVersionParts.Length > 1 ? apiVersionParts[1] : apiVersionParts[0];

            return new V1APIResource(kind, name, namespaced, "", Array.Empty<string>(), group: group, version: version);
        }

        private static string ExtractMajorMinorVersion(string providerVersion)
        {
            var parsedVersion = Version.Parse(providerVersion);

            return $"{parsedVersion.Major}.{parsedVersion.Minor}";
        }

        private static ErrorResponseException UnknownResourceTypeException(string resourceType) =>
            new("UnknownResourceType", $"Unknown resource type '{resourceType}'.", JsonPointer.Create("type"));
    }
}
