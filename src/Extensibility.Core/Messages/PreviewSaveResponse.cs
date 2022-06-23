// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Extensibility.Core.Messages
{
    using Extensibility.Core.Data;

    public class PreviewSaveResponse
    {
        public ExtensibleResourceBody? Body { get; set; }

        public ExtensibilityErrorContainer? Error { get; set; }
    }
}
