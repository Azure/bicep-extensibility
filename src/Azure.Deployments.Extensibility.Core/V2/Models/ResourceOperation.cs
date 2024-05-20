// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
