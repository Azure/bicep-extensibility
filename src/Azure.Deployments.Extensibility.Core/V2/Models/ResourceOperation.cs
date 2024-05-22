// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace Azure.Deployments.Extensibility.Core.V2.Models
{
    public record ResourceOperation
    {
        public ResourceOperation()
        {
        }

        [SetsRequiredMembers]
        public ResourceOperation(string status, Error? error)
        {
            this.Status = status;
            this.Error = error;
        }

        public required string Status { get; init; }

        public Error? Error { get; init; }
    }
}
