// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace Azure.Deployments.Extensibility.Core.V2.Models
{
    public class ErrorData
    {
        public ErrorData()
        {
        }

        [SetsRequiredMembers]
        public ErrorData(Error error)
        {
            this.Error = error;
        }

        public required Error Error { get; init; }
    }
}
