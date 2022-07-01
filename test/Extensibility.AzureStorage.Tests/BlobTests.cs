// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Extensibility.AzureStorage.Tests
{
    using System;
    using System.Text;
    using System.Text.Json.Nodes;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility.Tests;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class BlobTests
    {
        [TestMethod]
        public async Task Save_blob()
        {
            // Blob requires a container first - create it here.
            await CrudHelper.Save(new()
            {
                Type = "container",
                Import = TestHelper.BuildImport(),
                Properties = new JsonObject
                {
                    ["name"] = "mrblobby",
                }
            }, CancellationToken.None);

            await CrudHelper.Save(new()
            {
                Type = "blob",
                Import = TestHelper.BuildImport(),
                Properties = new JsonObject
                {
                    ["containerName"] = "mrblobby",
                    ["name"] = "testfile.txt",
                    ["base64Content"] = Convert.ToBase64String(Encoding.UTF8.GetBytes("Extensibility Test!")),
                }
            }, CancellationToken.None);
        }
    }
}
