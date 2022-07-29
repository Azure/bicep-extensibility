// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Extensibility.Core.Extensions
{
    public static class JsonNodeExtensions
    {
        public static T ToObject<T>(this JsonNode value)
        {
            var stringValue = JsonSerializer.Serialize(value);

            return JsonSerializer.Deserialize<T>(stringValue)!;
        }
    }
}