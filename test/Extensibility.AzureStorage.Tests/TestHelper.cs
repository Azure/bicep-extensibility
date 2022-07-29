// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Extensibility.AzureStorage.Tests
{
    using System.Text.Json.Nodes;
    using Extensibility.Core.Data;

    public static class TestHelper
    {
        public static ExtensibleImport BuildImport(string connectionString = "UseDevelopmentStorage=true")
        {
            // TODO find a way to persist connection string settings securely, to avoid relying on the storage emulator
            return new()
            {
                Provider = "AzureStorage",
                Version = "0.1",
                Config = new JsonObject
                {
                    ["connectionString"] = connectionString,
                },
            };
        }
    }
}
