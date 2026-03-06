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
        public FilteredJsonObject(JsonObject filteredObject, JsonObject? removedObject = null, IList<JsonPointer>? arrayPaths = null)
        {
            this.FilteredObject = filteredObject;
            this.RemovedObject = removedObject;
            this.ArrayPaths = arrayPaths;
        }

        /// <summary>
        /// The filtered object is a reflection of the original object without nodes that were removed.
        /// </summary>
        public required JsonObject FilteredObject { get; init; }

        /// <summary>
        /// A reflection of the original object but with only nodes that were removed (and their partial parent nodes).
        /// Arrays in this object are stored as dictionaries of array index to value. The array index is the insertion index for the value.
        /// </summary>
        public JsonObject? RemovedObject { get; init; }

        /// <summary>
        /// All array paths present in the original object that have at least 1 child node removed. The list is sorted in index ascending
        /// order.
        /// </summary>
        public IList<JsonPointer>? ArrayPaths { get; init; }
    }
}
