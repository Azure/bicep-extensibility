// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Json.Pointer;

namespace Azure.Deployments.Extensibility.Core.V2.Json
{
    public readonly record struct JsonPointerProxy(string Source)
    {
        public static implicit operator JsonPointerProxy(string source) => new(source);

        public JsonPointer ToJsonPointer() => JsonPointer.Parse(this.Source);
    }
}
