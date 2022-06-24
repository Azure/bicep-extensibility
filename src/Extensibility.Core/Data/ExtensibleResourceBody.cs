// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json.Linq;

namespace Extensibility.Core.Data
{
    public class ExtensibleResourceBody
    {
        public ExtensibleImport? Import { get; set; }

        public string? Type { get; set; }

        public JToken? Properties { get; set; }
    }
}
