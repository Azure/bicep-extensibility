// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CsvHelper;
using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;

namespace Azure.Deployments.Extensibility.Extensions.Kubernetes.Api.ApiCatalog
{
    internal class K8sApiCatalog
    {
        private const string FileName = $"{nameof(K8sApiCatalog)}.csv";

        private static readonly Lazy<K8sApiCatalog> lazyInstance = new(() => new(ReadRecordsFromEmbeddedResource()));

        private readonly ImmutableArray<K8sApiMetadata> records;

        public K8sApiCatalog(ImmutableArray<K8sApiMetadata> records)
        {
            this.records = records;
        }

        public static K8sApiCatalog Instance => lazyInstance.Value;

        private static ImmutableArray<K8sApiMetadata> ReadRecordsFromEmbeddedResource()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var csvResourceName = $"{assembly.GetName().Name}.Api.ApiCatalog.{FileName}";
            var csvStream = assembly.GetManifestResourceStream(csvResourceName) ?? throw new InvalidOperationException($"Unable to load {csvResourceName} from assembly.");

            using var csvStreamReader = new StreamReader(csvStream);
            using var csvReader = new CsvReader(csvStreamReader, CultureInfo.InvariantCulture);
            csvReader.Context.TypeConverterCache.AddConverter<ImmutableArray<string>>(ImmutableArrayConverter.Instance);

            return csvReader.GetRecords<K8sApiMetadata>().ToImmutableArray();
        }

        public K8sApiMetadata? TryFindMatchingRecord(K8sApiMetadata apiMetadata)
        {
            var matchingIndex = this.records.BinarySearch(apiMetadata);

            return matchingIndex >= 0 ? this.records[matchingIndex] : null;
        }

        public K8sApiCatalog Merge(ImmutableArray<K8sApiMetadata> additionalRecords)
        {
            var mergedRecords = this.records
                .Concat(additionalRecords)
                .GroupBy(x => (x.Group, x.Version, x.Kind, x.Plural, x.Namespaced))
                .Select(g => new K8sApiMetadata(
                    g.Key.Group,
                    g.Key.Version,
                    g.Key.Kind,
                    g.Key.Plural,
                    g.Key.Namespaced,
                    g.SelectMany(x => x.MajorMinorServerVersions).Distinct().ToImmutableArray().Sort()))
                .Order()
                .ToImmutableArray();

            return new K8sApiCatalog(mergedRecords);
        }

        public void WriteRecordsToFile()
        {
            using var streamWriter = new StreamWriter(GetFilePath());
            using var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture);
            csvWriter.Context.TypeConverterCache.AddConverter<ImmutableArray<string>>(ImmutableArrayConverter.Instance);

            csvWriter.WriteRecords(this.records);
        }

        private static string GetFilePath()
        {
            var repoRootDirectory = new DirectoryInfo(Environment.CurrentDirectory);

            while (repoRootDirectory.Parent is { } parentDirectory)
            {
                // search upwards for the .git directory. This should only exist at the repository root.
                if (Directory.Exists(Path.Join(repoRootDirectory.FullName, ".git")))
                {
                    return Path.Combine(repoRootDirectory.FullName, "src", $"{Assembly.GetExecutingAssembly().GetName().Name}", "Data", FileName);
                }

                repoRootDirectory = parentDirectory;
            }

            throw new InvalidOperationException($"Unable to determine the repo root path from directory {Environment.CurrentDirectory}");
        }
    }
}
