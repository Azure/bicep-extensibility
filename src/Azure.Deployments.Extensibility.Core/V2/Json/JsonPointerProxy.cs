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

        /// <summary>
        /// Initializes a new instance by parsing the specified JSON Pointer string.
        /// </summary>
        /// <param name="source">A JSON Pointer string (e.g., "/properties/name").</param>
        public JsonPointerProxy(string source)
        {
            this.pointer = JsonPointer.Parse(source);
        }
        
        /// <summary>
        /// Initializes a new instance by wrapping an existing <see cref="JsonPointer"/>.
        /// </summary>
        /// <param name="pointer">The JSON Pointer to wrap.</param>
        public JsonPointerProxy(JsonPointer pointer)
        {
            this.pointer = pointer;
        }

        /// <summary>Implicitly converts a string to a <see cref="JsonPointerProxy"/>.</summary>
        public static implicit operator JsonPointerProxy(string source) => new(source);

        /// <summary>Implicitly converts a <see cref="JsonPointer"/> to a <see cref="JsonPointerProxy"/>.</summary>
        public static implicit operator JsonPointerProxy(JsonPointer pointer) => new(pointer);

        /// <summary>Implicitly converts a <see cref="JsonPointerProxy"/> to its string representation.</summary>
        public static implicit operator string(JsonPointerProxy proxy) => proxy.pointer.ToString();

        /// <summary>Implicitly converts a <see cref="JsonPointerProxy"/> to a <see cref="JsonPointer"/>.</summary>
        public static implicit operator JsonPointer(JsonPointerProxy proxy) => proxy.pointer;

        /// <summary>
        /// Converts this proxy to the underlying <see cref="JsonPointer"/>.
        /// </summary>
        /// <returns>The underlying <see cref="JsonPointer"/> value.</returns>
        public JsonPointer ToJsonPointer() => this.pointer;
    }
}
