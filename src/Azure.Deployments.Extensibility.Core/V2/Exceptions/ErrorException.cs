// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Models;
using Json.Pointer;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Core.V2.Exceptions
{
    public class ErrorException
    {
        public ErrorException(Error error)
        {
            this.Error = error;
        }

        public ErrorException(string code, string message, JsonPointer? target = null, IList<ErrorDetail>? details = null, JsonObject? innerError = null)
            : this(new(code, message, target, details, innerError))
        {

        }

        public Error Error { get; }

        public ErrorData ToErrorData() => new(this.Error);
    }
}
