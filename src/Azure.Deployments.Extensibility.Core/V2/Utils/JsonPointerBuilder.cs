// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Json.Pointer;
using System.Linq.Expressions;
using System.Text.Json;

namespace Azure.Deployments.Extensibility.Core.V2.Utils
{
    public static class JsonPointerBuilder
    {
        public static JsonPointer Build<T>(Expression<Func<T, object>> expression)
        {
            var segmentsInCamelCase = JsonPointer.Create(expression).Segments
                .Select(x => JsonNamingPolicy.CamelCase.ConvertName(x.Value))
                .Select(PointerSegment.Create);

            return JsonPointer.Create(segmentsInCamelCase);
        }
    }
}
