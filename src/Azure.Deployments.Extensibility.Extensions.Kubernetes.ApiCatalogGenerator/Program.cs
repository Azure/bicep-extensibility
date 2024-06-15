// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.


using Azure.Deployments.Extensibility.Extensions.Kubernetes.Api.ApiCatalog;
using k8s;
using k8s.Models;
using System.Collections.Immutable;

var configuration = KubernetesClientConfiguration.BuildConfigFromConfigFile();
using var kubernetes = new Kubernetes(configuration);

var serverVersion = await kubernetes.Version.GetCodeAsync(CancellationToken.None);
var majorMinorServerVersion = $"{serverVersion.Major}.{serverVersion.Minor}";

var coreApiVersions = await kubernetes.Core.GetAPIVersionsAsync();
var namedApiVersions = await kubernetes.Apis.GetAPIVersionsAsync();

var groupVersions = coreApiVersions.Versions
    .Select(x => (Group: "", Version: x))
    .Concat(namedApiVersions.Groups
        .SelectMany(g => g.Versions
            .Select(v => (Group: g.Name, v.Version))));

var apiCatalogBuilder = ImmutableArray.CreateBuilder<K8sApiMetadata>();

foreach (var (group, version) in groupVersions)
{
    var client = new GenericClient(kubernetes, group, version, plural: "", disposeClient: false);
    var apiResourceList = await client.ListAsync<V1APIResourceList>();

    // Excluding subresources by filtering out names containing '/'.
    foreach (var apiResource in apiResourceList.Resources.Where(x => !x.Name.Contains('/')))
    {
        apiCatalogBuilder.Add(new K8sApiMetadata(group, version, apiResource.Kind, apiResource.Name, apiResource.Namespaced, [majorMinorServerVersion]));
    }
}

var apiRecordsForCurrentMajorMinorServerVersion = apiCatalogBuilder.DrainToImmutable();

K8sApiCatalog.Instance.Merge(apiRecordsForCurrentMajorMinorServerVersion).WriteRecordsToFile();
