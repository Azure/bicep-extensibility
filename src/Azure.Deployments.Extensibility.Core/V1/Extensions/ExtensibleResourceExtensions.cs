// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Json.Pointer;
using System.Linq.Expressions;

namespace Azure.Deployments.Extensibility.Core.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="ExtensibleResource{TProperty}"/>.
    /// </summary>
    public static class ExtensibleResourceExtensions
    {
        /// <summary>
        /// Build a JSON Pointer rooted at the resource object for the specified property.
        /// </summary>
        public static JsonPointer GetJsonPointer<TProperty>(this ExtensibleResource<TProperty> resource, Expression<Func<ExtensibleResource<TProperty>, object>> expression) =>
            JsonPointer.Create(nameof(resource)).Combine(expression.ToJsonPointer());

    }
}
