// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Json.Pointer;
using System.Linq.Expressions;

namespace Azure.Deployments.Extensibility.Core.Extensions
{
    public static class ExpressionExtensions
    {
        public static JsonPointer ToJsonPointer<T>(this Expression<Func<T, object>> expression) =>
            JsonPointer.Create(expression, new PointerCreationOptions
            {
                PropertyNameResolver = PropertyNameResolvers.CamelCase,
            });
    }
}
