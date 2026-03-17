// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Json.Pointer;

namespace Azure.Deployments.Extensibility.Core.V2.Json
{
    /// <summary>
    /// A lightweight wrapper around a JSON Pointer string that supports implicit conversion from <see cref="string"/>.
    /// </summary>
    public readonly record struct JsonPointerProxy
    {
        private readonly JsonPointer pointer;
        
        public JsonPointerProxy(string source)
        {
            this.pointer = JsonPointer.Parse(source);
        }
        
        public JsonPointerProxy(JsonPointer pointer)
        {
            this.pointer = pointer;
        }

        public static implicit operator JsonPointerProxy(string source) => new(source);

        public static implicit operator JsonPointerProxy(JsonPointer pointer) => new(pointer);

        public static implicit operator string(JsonPointerProxy proxy) => proxy.pointer.ToString();

        public static implicit operator JsonPointer(JsonPointerProxy proxy) => proxy.pointer;
        public JsonPointer ToJsonPointer() => this.pointer;
    }
}
