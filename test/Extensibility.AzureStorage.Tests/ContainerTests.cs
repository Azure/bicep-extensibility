// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Extensibility.AzureStorage.Tests
{
    using System.Text.Json.Nodes;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility.Tests;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ContainerTests
    {
        [TestMethod]
        public async Task Save_container()
        {
            await CrudHelper.Save(new()
            {
                Type = "container",
                Import = TestHelper.BuildImport(),
                Properties = new JsonObject
                {
                    ["name"] = "test123",
                }
            }, CancellationToken.None);
        }
    }
}
