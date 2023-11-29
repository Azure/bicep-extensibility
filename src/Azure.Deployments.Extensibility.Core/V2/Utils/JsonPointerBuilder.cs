// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Json.Pointer;
using System.Linq.Expressions;

namespace Azure.Deployments.Extensibility.Core.V2.Utils
{
    public static class JsonPointerBuilder
    {
        private static readonly PointerCreationOptions DefaultOptions = new()
        {
            PropertyNameResolver = PropertyNameResolvers.CamelCase,
        };

        public static JsonPointer Build<T>(Expression<Func<T, object>> expression, PointerCreationOptions? options = null) =>
            JsonPointer.Create(expression, options ?? DefaultOptions);
    }
}
