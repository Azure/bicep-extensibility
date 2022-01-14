namespace Extensibility.Core.Messages
{
    using Extensibility.Core.Data;

    public class SaveResponse
    {
        public ExtensibleResourceBody? Body { get; set; }

        public ExtensibilityErrorContainer? Error { get; set; }
    }
}
