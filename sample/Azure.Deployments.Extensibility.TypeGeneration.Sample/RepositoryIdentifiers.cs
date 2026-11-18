// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Bicep.Types.Concrete;
using Azure.Deployments.Extensibility.TypeGeneration.Attributes;

namespace Contoso.Models;

public class RepositoryIdentifiers
{
    [TypeProperty("Repository name", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public required string Name { get; set; }
}
