// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using System.Collections.Immutable;

namespace Azure.Deployments.Extensibility.Extensions.Kubernetes.Api.ApiCatalog
{
    internal class ImmutableArrayConverter : ITypeConverter
    {
        private readonly static Lazy<ImmutableArrayConverter> LazyInstance = new(() => new());

        private ImmutableArrayConverter() { }

        public static ImmutableArrayConverter Instance => LazyInstance.Value;

        public object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData) =>
            string.IsNullOrEmpty(text) ? [] : ImmutableArray.Create(text.Split("|"));

        public string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
        {
            if (value is not ImmutableArray<string> array)
            {
                return null;
            }

            return string.Join('|', array);
        }
    }
}
