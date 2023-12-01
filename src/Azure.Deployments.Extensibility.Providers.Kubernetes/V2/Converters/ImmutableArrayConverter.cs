// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using System.Collections.Immutable;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Converters
{
    public class ImmutableArrayConverter : ITypeConverter
    {
        private readonly static Lazy<ImmutableArrayConverter> LazyInstance = new(() => new());

        private ImmutableArrayConverter() { }

        public static ImmutableArrayConverter Instance => LazyInstance.Value;

        public object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData) =>
            string.IsNullOrEmpty(text) ? ImmutableArray<string>.Empty : ImmutableArray.Create(text.Split("|"));

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
