// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Converters;
using Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Models;
using Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Services;
using CsvHelper;
using k8s;
using k8s.Models;
using System.Collections.Immutable;
using System.Globalization;


internal class Program
{
    private static async Task Main(string[] args)
    {
        var apiCatalogForCurrentMajorMinorServerVersion = await GenerateApiCatalogForCurrentMajorMinorServerVersionAsync();

        var apiCatalog = K8sApiDiscoveryService.LoadK8sApiCatalog()
            .Concat(apiCatalogForCurrentMajorMinorServerVersion)
            .GroupBy(x => (x.Group, x.Version, x.Kind, x.Plural, x.Namespaced))
            .Select(g => new K8sApiMetadata(
                g.Key.Group,
                g.Key.Version,
                g.Key.Kind,
                g.Key.Plural,
                g.Key.Namespaced,
                g.SelectMany(x => x.MajorMinorServerVersions).Distinct().ToImmutableArray().Sort()))
            .Order()
            .ToArray();


        using var streamWriter = new StreamWriter(GetApiCatalogCsvFilePath());
        using var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture);
        csvWriter.Context.TypeConverterCache.AddConverter<ImmutableArray<string>>(ImmutableArrayConverter.Instance);

        csvWriter.WriteRecords(apiCatalog);
    }

    private static async Task<IReadOnlyList<K8sApiMetadata>> GenerateApiCatalogForCurrentMajorMinorServerVersionAsync()
    {
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

        var apiCatalog = new List<K8sApiMetadata>();

        foreach (var (group, version) in groupVersions)
        {
            var client = new GenericClient(kubernetes, group, version, plural: "", disposeClient: false);
            var apiResourceList = await client.ListAsync<V1APIResourceList>();

            // Excluding sub-resources by filtering out names containing '/'.
            foreach (var apiResource in apiResourceList.Resources.Where(x => !x.Name.Contains('/')))
            {
                apiCatalog.Add(new K8sApiMetadata(group, version, apiResource.Kind, apiResource.Name, apiResource.Namespaced, ImmutableArray.Create(majorMinorServerVersion)));
            }
        }

        return apiCatalog;
    }

    private static string GetApiCatalogCsvFilePath()
    {
        var repoRootDirectory = new DirectoryInfo(Environment.CurrentDirectory);

        while (repoRootDirectory.Parent is { } parentDirectory)
        {
            // search upwards for the .git directory. This should only exist at the repository root.
            if (Directory.Exists(Path.Join(repoRootDirectory.FullName, ".git")))
            {
                return Path.Combine(repoRootDirectory.FullName, "src", "Azure.Deployments.Extensibility.Providers.Kubernetes", "V2", "Data", "K8sApiCatalog.csv");
            }

            repoRootDirectory = parentDirectory;
        }

        throw new InvalidOperationException($"Unable to determine the repo root path from directory {Environment.CurrentDirectory}");
    }
}
