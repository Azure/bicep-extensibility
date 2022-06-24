// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json.Linq;

namespace Extensibility.Core.Data
{
    public class ExtensibleImport
    {
        public string? Provider { get; set; }

        public string? Version { get; set; }

        public JObject? Config { get; set; }
    }
}
