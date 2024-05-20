// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace Azure.Deployments.Extensibility.Core.V2.Models
{
    public class ErrorPayload
    {
        public ErrorPayload()
        {
        }

        [SetsRequiredMembers]
        public ErrorPayload(Error error)
        {
            this.Error = error;
        }

        public required Error Error { get; init; }
    }
}
