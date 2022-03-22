namespace Extensibility.Core.Data
{
    using System.Text.Json.Nodes;

    public class ExtensibleImport
    {
        public string? Provider { get; set; }

        public string? Version { get; set; }

        public JsonObject? Config { get; set; }
    }
}
