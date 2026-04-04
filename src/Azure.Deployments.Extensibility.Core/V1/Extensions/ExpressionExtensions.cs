// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Json.Pointer;
using System.Linq.Expressions;

namespace Azure.Deployments.Extensibility.Core.Extensions
{
    /// <summary>
    /// Extension methods for converting LINQ expressions to JSON Pointers.
    /// </summary>
    public static class ExpressionExtensions
    {
        /// <summary>
        /// Convert a property access expression to a camelCase JSON Pointer.
        /// </summary>
        /// <typeparam name="T">The source type.</typeparam>
        /// <param name="expression">The property access expression.</param>
        /// <returns>A <see cref="JsonPointer"/> corresponding to the property path.</returns>
        public static JsonPointer ToJsonPointer<T>(this Expression<Func<T, object>> expression) =>
            JsonPointer.Create(expression, new PointerCreationOptions
            {
                PropertyNameResolver = PropertyNameResolvers.CamelCase,
            });
    }
}
