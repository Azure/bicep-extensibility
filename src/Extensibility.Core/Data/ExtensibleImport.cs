// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;

namespace Extensibility.Core.Data
{
    public class ExtensibleImport
    {
        public string? Provider { get; set; }

        public string? Version { get; set; }

        public JsonObject? Config { get; set; }
    }
}
