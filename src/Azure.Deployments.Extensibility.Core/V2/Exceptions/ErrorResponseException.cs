// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Models;
using Json.Pointer;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Core.V2.Exceptions
{
    public class ErrorResponseException(Error error) : Exception
    {
        public ErrorResponseException(string code, string message, JsonPointer? target = null, IReadOnlyList<ErrorDetail>? details = null, JsonObject? innerError = null)
            : this(new(code, message, target, details, innerError))
        {

        }

        public Error Error => error;

        public ErrorResponseBody ToErrorResponseBody() => new(error);
    }
}
