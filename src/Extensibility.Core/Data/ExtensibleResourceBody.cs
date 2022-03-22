namespace Extensibility.Core.Data
{
    using System.Text.Json.Nodes;

    public class ExtensibleResourceBody
    {
        public ExtensibleImport? Import { get; set; }

        public string? Type { get; set; }

        public JsonNode? Properties { get; set; }
    }
}
