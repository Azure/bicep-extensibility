// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace Azure.Deployments.Extensibility.Core.V2.Models
{
    public class FilteredPreviewResourceSpecification
    {
        public FilteredPreviewResourceSpecification() { }

        [SetsRequiredMembers]
        public FilteredPreviewResourceSpecification(ResourceSpecification filteredSpec, FilteredJsonObject? filteredProperties = null, FilteredJsonObject? filteredConfig = null)
        {
            this.FilteredSpec = filteredSpec;
            this.FilteredProperties = filteredProperties;
            this.FilteredConfig = filteredConfig;
        }

        public required ResourceSpecification FilteredSpec { get; set; }

        public FilteredJsonObject? FilteredProperties { get; set; }

        public FilteredJsonObject? FilteredConfig { get; set; }

        // TODO(kylealbert): Add a method to get the merged resource spec. Use JsonDiffPatch to generate a patch that can be used (and potentially modified)
        //  and apply the patch.
    }
}
