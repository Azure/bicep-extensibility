// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Json.Pointer;
using System.Linq.Expressions;

namespace Azure.Deployments.Extensibility.Core.Extensions
{
    public static class ExtensibleImportExtensions
    {
        public static JsonPointer GetJsonPointer<TConfig>(this ExtensibleImport<TConfig> import, Expression<Func<ExtensibleImport<TConfig>, object>> expression) =>
            JsonPointer.Create(nameof(import)).Combine(expression.ToJsonPointer());
    }
}
