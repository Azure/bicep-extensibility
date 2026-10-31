// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;

namespace Azure.Deployments.Extensibility.TypeGeneration.Semantics;

public sealed record ResourceTypeDefinition
{
    public required string Name { get; init; }

    public required ImmutableArray<string> ApiVersions { get; init; }

    public required ResourceScope ReadableScopes { get; init; }

    public required ResourceScope WritableScopes { get; init; }

    public required TypeDefinition Body { get; init; }

    public required ImmutableArray<ResourceFunctionDefinition> ResourceFunctions { get; init; }

    public SourceLocation? SourceLocation { get; init; }
}

public sealed record ProjectedResourceTypeDefinition
{
    public required string Name { get; init; }

    public string? ApiVersion { get; init; }

    public required ResourceScope ReadableScopes { get; init; }

    public required ResourceScope WritableScopes { get; init; }

    public required TypeDefinition Body { get; init; }

    public required ImmutableArray<ProjectedResourceFunctionDefinition> ResourceFunctions { get; init; }

    public SourceLocation? SourceLocation { get; init; }
}

[Flags]
public enum ResourceScope
{
    None = 0,
    Tenant = 1 << 0,
    ManagementGroup = 1 << 1,
    Subscription = 1 << 2,
    ResourceGroup = 1 << 3,
    Extension = 1 << 4,
    AllExceptExtension = Tenant | ManagementGroup | Subscription | ResourceGroup,
    All = AllExceptExtension | Extension,
}
