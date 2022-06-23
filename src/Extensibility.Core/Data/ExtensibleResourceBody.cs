// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Extensibility.Core.Data
{
    using Newtonsoft.Json.Linq;

    public class ExtensibleResourceBody
    {
        public ExtensibleImport? Import { get; set; }

        public string? Type { get; set; }

        public JToken? Properties { get; set; }
    }
}
