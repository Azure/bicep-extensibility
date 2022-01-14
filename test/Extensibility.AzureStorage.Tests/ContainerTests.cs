namespace Extensibility.AzureStorage.Tests
{
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility.Tests;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json.Linq;

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
                Properties = new JObject
                {
                    ["name"] = "test123",
                }
            }, CancellationToken.None);
        }
    }
}
