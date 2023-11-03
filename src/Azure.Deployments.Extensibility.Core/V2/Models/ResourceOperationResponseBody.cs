// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace Azure.Deployments.Extensibility.Core.V2.Models
{
    public record ResourceOperationResponseBody
    {
        public ResourceOperationResponseBody()
        {
        }

        [SetsRequiredMembers]
        public ResourceOperationResponseBody(string status, Error? error = null)
        {
            this.Status = status;
            this.Error = error;
        }

        public required string Status { get; init; }

        public Error? Error { get; init; }
    }
}
