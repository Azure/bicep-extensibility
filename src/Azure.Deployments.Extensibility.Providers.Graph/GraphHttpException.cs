// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using Azure.Deployments.Extensibility.Core.Exceptions;
using Json.Pointer;

namespace Azure.Deployments.Extensibility.Providers.Graph
{
    public class GraphHttpException : Exception
    {
        public GraphHttpException(int statusCode, string message) : base(message)
        {
            this.StatusCode = statusCode;
        }

        public int StatusCode
        {
            get;
            private set;
        }

        public ExtensibilityException ToExtensibilityException()
        {
            return new ExtensibilityException(
                this.StatusCode.ToString(),
                JsonPointer.Empty,
                this.Message
            );
        }
    }
}

