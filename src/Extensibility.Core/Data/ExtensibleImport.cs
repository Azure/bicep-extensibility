namespace Extensibility.Core.Data
{
    using Newtonsoft.Json.Linq;

    public class ExtensibleImport
    {
        public string? Provider { get; set; }

        public string? Version { get; set; }

        public JObject? Config { get; set; }
    }
}
