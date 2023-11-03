// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Json.Pointer;
using System.Linq.Expressions;
using System.Text.Json;

namespace Azure.Deployments.Extensibility.Core.Extensions
{
    public static class ExpressionExtensions
    {
        public static JsonPointer ToJsonPointer<T>(this Expression<Func<T, object>> expression) => JsonPointer.Create(
            JsonPointer.Create(expression).Segments
                .Select(x => JsonNamingPolicy.CamelCase.ConvertName(x.Value))
                .Select(PointerSegment.Create));
    }
}
