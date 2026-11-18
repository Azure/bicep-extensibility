// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Bicep.Types.Concrete;
using Azure.Deployments.Extensibility.TypeGeneration.Attributes;

namespace GeneratedModels;

[ResourceType("Repository", "2026-10-01")]
public partial class Repository
{
    [TypeProperty("Handwritten name", ObjectTypePropertyFlags.Required | ObjectTypePropertyFlags.Identifier)]
    public required string Name { get; set; }
}
