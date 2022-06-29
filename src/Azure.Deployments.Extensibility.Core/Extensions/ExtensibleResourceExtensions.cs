// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Json.Pointer;
using System.Linq.Expressions;

namespace Azure.Deployments.Extensibility.Core.Extensions
{
    public static class ExtensibleResourceExtensions
    {
        public static JsonPointer GetJsonPointer<TProperty>(this ExtensibleResource<TProperty> resource, Expression<Func<ExtensibleResource<TProperty>, object>> expression) =>
            JsonPointer.Create(nameof(resource)).Combine(expression.ToJsonPointer());

    }
}
