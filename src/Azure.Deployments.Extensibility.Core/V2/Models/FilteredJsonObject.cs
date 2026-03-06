// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Json.Pointer;

namespace Azure.Deployments.Extensibility.Core.V2.Models
{
    public record FilteredJsonObject
    {
        public FilteredJsonObject() { }

        [SetsRequiredMembers]
        public FilteredJsonObject(JsonObject filteredObject, JsonObject? removedObject = null, ISet<JsonPointer>? arrayPaths = null)
        {
            this.FilteredObject = filteredObject;
            this.RemovedObject = removedObject;
            this.ArrayPaths = arrayPaths;
        }

        public required JsonObject FilteredObject { get; init; }

        public JsonObject? RemovedObject { get; init; }

        public ISet<JsonPointer>? ArrayPaths { get; init; }
    }
}
