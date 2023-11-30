// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Exceptions;
using Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Converters;
using Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Models;
using CsvHelper;
using Json.Pointer;
using k8s;
using k8s.Autorest;
using k8s.Models;
using System.Collections.Immutable;
using System.Globalization;
using System.Net;
using System.Reflection;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Services
{
    public class K8sApiDiscoveryService(IKubernetes kubernetes) : IK8sApiDiscoveryService
    {
        private readonly static ImmutableArray<K8sApiMetadata> K8sApiCatalog = LoadK8sApiCatalog();

        public static ImmutableArray<K8sApiMetadata> LoadK8sApiCatalog()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var assemblyName = assembly.GetName().Name;

            var csvResourceName = $"{assemblyName}.V2.Data.K8sApiCatalog.csv";
            var csvStream = assembly.GetManifestResourceStream(csvResourceName) ?? throw new InvalidOperationException($"Unable to load {csvResourceName} from assembly.");

            using var csvStreamReader = new StreamReader(csvStream);
            using var csvReader = new CsvReader(csvStreamReader, CultureInfo.InvariantCulture);
            csvReader.Context.TypeConverterCache.AddConverter<ImmutableArray<string>>(ImmutableArrayConverter.Instance);

            return csvReader.GetRecords<K8sApiMetadata>().ToImmutableArray();
        }

        public async Task<K8sApiMetadata> FindK8sApiMetadataAsync(string providerVersion, K8sResourceType resourceType, CancellationToken cancellationToken)
        {
            var (group, version, kind) = resourceType;
            var matchingIndex = K8sApiCatalog.BinarySearch(new K8sApiMetadata(group, version, kind, "", default, default));

            if (matchingIndex >= 0)
            {
                var matchingApiMetadata = K8sApiCatalog[matchingIndex];
                var majorMinorVersion = ExtractMajorMinorVersion(providerVersion);
                
                if (matchingApiMetadata.MajorMinorServerVersions.BinarySearch(majorMinorVersion) >= 0)
                {
                    return matchingApiMetadata;
                }
            }

            try
            {
                var client = new GenericClient(kubernetes, group, version, plural: "", disposeClient: false);
                var apiResourceList = await client.ListAsync<V1APIResourceList>(cancellationToken);

                if (apiResourceList.Resources.SingleOrDefault(x => x.Kind.Equals(kind, StringComparison.Ordinal)) is not { } apiResource)
                {
                    throw UnknownResourceTypeException(resourceType);
                }

                return new K8sApiMetadata(group, version, kind, apiResource.Name, apiResource.Namespaced, default);
            }
            catch (HttpOperationException exception) when (exception.Response.StatusCode == HttpStatusCode.NotFound)
            {
                throw UnknownResourceTypeException(resourceType);
            }
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
