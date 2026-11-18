// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Bicep.Types.Concrete;
using Azure.Deployments.Extensibility.TypeGeneration.Attributes;

namespace Contoso.Models;

[ResourceType("Repository", ApiVersions = new[] { "2026-08-01", "2026-09-01" })]
public sealed class Repository : RepositoryIdentifiers
{
    [TypeProperty("Description")]
    public string? Description { get; set; }

    [TypeProperty("URL", ObjectTypePropertyFlags.ReadOnly)]
    public string? Url { get; set; }

    [TypeProperty("Repository settings")]
    public RepositorySettings? Settings { get; set; }
}
