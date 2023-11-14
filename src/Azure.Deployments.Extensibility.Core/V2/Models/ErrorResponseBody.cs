// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace Azure.Deployments.Extensibility.Core.V2.Models
{
    // See http://docs.oasis-open.org/odata/odata-json-format/v4.0/os/odata-json-format-v4.0-os.html#_Toc372793091
    public record ErrorResponseBody
    {
        public ErrorResponseBody()
        {
        }

        [SetsRequiredMembers]
        public ErrorResponseBody(Error error)
        {
            this.Error = error;
        }

        public required Error Error { get; init; }
    }
}
