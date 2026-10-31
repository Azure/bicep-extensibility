// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Azure.Deployments.Extensibility.TypeGeneration.Semantics;

public abstract record TypeDefinition
{
    public SourceLocation? SourceLocation { get; init; }
}

public sealed record AnyTypeDefinition : TypeDefinition;

public sealed record NullTypeDefinition : TypeDefinition;

public sealed record BooleanTypeDefinition : TypeDefinition;

public sealed record IntegerTypeDefinition : TypeDefinition
{
    public long? MinValue { get; init; }

    public long? MaxValue { get; init; }
}

public sealed record StringTypeDefinition : TypeDefinition
{
    public bool Sensitive { get; init; }

    public long? MinLength { get; init; }

    public long? MaxLength { get; init; }

    public string? Pattern { get; init; }
}

public sealed record StringLiteralTypeDefinition : TypeDefinition
{
    public required string Value { get; init; }
}

public sealed record ArrayTypeDefinition : TypeDefinition
{
    public required TypeDefinition ItemType { get; init; }

    public long? MinLength { get; init; }

    public long? MaxLength { get; init; }

    public ArrayTypeDefinition()
    {
    }

    // Graph initializers may reference the node, but must not inspect unfinished children
    // or publish the graph before its outermost constructor has returned.
    [SetsRequiredMembers]
    internal ArrayTypeDefinition(Func<ArrayTypeDefinition, TypeDefinition> initialize)
    {
        ArgumentNullException.ThrowIfNull(initialize);
        ItemType = initialize(this)
            ?? throw new ArgumentException("The initializer must return an item type.", nameof(initialize));
    }
}

public sealed record ObjectTypeDefinition : TypeDefinition
{
    public required string Name { get; init; }

    public required ImmutableArray<PropertyDefinition> Properties { get; init; }

    public TypeDefinition? AdditionalProperties { get; init; }

    public bool Sensitive { get; init; }

    public ObjectTypeDefinition()
    {
    }

    [SetsRequiredMembers]
    internal ObjectTypeDefinition(
        string name,
        Func<ObjectTypeDefinition, (ImmutableArray<PropertyDefinition> Properties, TypeDefinition? AdditionalProperties)> initialize)
    {
        ArgumentNullException.ThrowIfNull(initialize);
        Name = name;
        var contents = initialize(this);
        if (contents.Properties.IsDefault)
        {
            throw new ArgumentException("The initializer must return an initialized property array.", nameof(initialize));
        }

        Properties = contents.Properties;
        AdditionalProperties = contents.AdditionalProperties;
    }
}

public sealed record DiscriminatedObjectTypeDefinition : TypeDefinition
{
    public required string Name { get; init; }

    public required string Discriminator { get; init; }

    public required ImmutableArray<PropertyDefinition> BaseProperties { get; init; }

    public required ImmutableDictionary<string, ObjectTypeDefinition> Cases { get; init; }

    public DiscriminatedObjectTypeDefinition()
    {
    }

    [SetsRequiredMembers]
    internal DiscriminatedObjectTypeDefinition(
        string name,
        string discriminator,
        Func<DiscriminatedObjectTypeDefinition, (ImmutableArray<PropertyDefinition> BaseProperties, ImmutableDictionary<string, ObjectTypeDefinition> Cases)> initialize)
    {
        ArgumentNullException.ThrowIfNull(initialize);
        Name = name;
        Discriminator = discriminator;
        var contents = initialize(this);
        if (contents.BaseProperties.IsDefault || contents.Cases is null)
        {
            throw new ArgumentException("The initializer must return initialized base properties and cases.", nameof(initialize));
        }

        BaseProperties = contents.BaseProperties;
        Cases = contents.Cases;
    }
}

public sealed record UnionTypeDefinition : TypeDefinition
{
    public required ImmutableArray<UnionElementDefinition> Elements { get; init; }

    public UnionTypeDefinition()
    {
    }

    [SetsRequiredMembers]
    internal UnionTypeDefinition(Func<UnionTypeDefinition, ImmutableArray<UnionElementDefinition>> initialize)
    {
        ArgumentNullException.ThrowIfNull(initialize);
        var elements = initialize(this);
        if (elements.IsDefault)
        {
            throw new ArgumentException("The initializer must return an initialized element array.", nameof(initialize));
        }

        Elements = elements;
    }
}

public sealed record UnionElementDefinition
{
    public required TypeDefinition Type { get; init; }

    public VersionAvailability Availability { get; init; } = new();

    public SourceLocation? SourceLocation { get; init; }
}
