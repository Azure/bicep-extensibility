// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;

namespace Extensibility.Core.Data
{
    public class ExtensibleResourceBody
    {
        public ExtensibleImport? Import { get; set; }

        public string? Type { get; set; }

        public JsonNode? Properties { get; set; }
    }
}
