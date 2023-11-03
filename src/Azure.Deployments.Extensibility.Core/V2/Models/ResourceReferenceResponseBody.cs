// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace Azure.Deployments.Extensibility.Core.V2.Models
{
    public record ResourceReferenceResponseBody
    {
        public ResourceReferenceResponseBody()
        {
        }

        [SetsRequiredMembers]
        public ResourceReferenceResponseBody(string referenceId)
        {
            this.ReferenceId = referenceId;
        }

        public required string ReferenceId { get; init; }
    }
}
