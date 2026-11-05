// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using Azure.Deployments.Extensibility.TypeGeneration.Semantics;
using Azure.Deployments.Extensibility.TypeGeneration.Tests.Support;
using Xunit;

namespace Azure.Deployments.Extensibility.TypeGeneration.Tests.Unit.Semantics;

[Trait("Category", "Unit")]
public class ModelGraphTests
{
    [Fact]
    public void SharedNodes_RetainReferenceIdentity()
    {
        var shared = new ObjectTypeDefinition { Name = "Settings", Properties = [] };
        var parent = new ObjectTypeDefinition
        {
            Name = "Parent",
            Properties =
            [
                new() { Name = "first", Type = shared },
                new() { Name = "second", Type = shared },
            ],
        };

        Assert.Same(parent.Properties[0].Type, parent.Properties[1].Type);
    }

    [Fact]
    public void ObjectInitializer_CanWireNullableSelfReference()
    {
        var calls = 0;
        var node = new ObjectTypeDefinition("Node", self =>
        {
            calls++;
            return ([
                new PropertyDefinition
                {
                    Name = "next",
                    Type = new UnionTypeDefinition
                    {
                        Elements =
                        [
                            new() { Type = self },
                            new() { Type = new NullTypeDefinition() },
                        ],
                    },
                },
            ], null);
        });
        var nullable = Assert.IsType<UnionTypeDefinition>(Assert.Single(node.Properties).Type);

        Assert.Equal(1, calls);
        Assert.Same(node, nullable.Elements[0].Type);
        Assert.IsType<NullTypeDefinition>(nullable.Elements[1].Type);
    }

    [Fact]
    public void ObjectInitializer_CanWireMutualReferences()
    {
        var first = new ObjectTypeDefinition("First", self =>
        {
            var second = new ObjectTypeDefinition("Second", _ =>
                ([new PropertyDefinition { Name = "first", Type = self }], null));
            return ([new PropertyDefinition { Name = "second", Type = second }], null);
        });
        var second = Assert.IsType<ObjectTypeDefinition>(Assert.Single(first.Properties).Type);

        Assert.Equal("Second", second.Name);
        Assert.Same(first, Assert.Single(second.Properties).Type);
    }

    [Fact]
    public void AdditionalProperties_CanReferenceTheirOwningObject()
    {
        var dictionary = new ObjectTypeDefinition("RecursiveMap", self => ([], self));

        Assert.Empty(dictionary.Properties);
        Assert.Same(dictionary, dictionary.AdditionalProperties);
    }

    [Fact]
    public void ArrayInitializer_CanWireSelfReference()
    {
        var calls = 0;
        var array = new ArrayTypeDefinition(self =>
        {
            calls++;
            return self;
        });

        Assert.Equal(1, calls);
        Assert.Same(array, array.ItemType);
    }

    [Fact]
    public void UnionInitializer_CanWireSelfReference()
    {
        var union = new UnionTypeDefinition(self =>
        [
            new() { Type = self },
            new() { Type = new NullTypeDefinition() },
        ]);

        Assert.Same(union, union.Elements[0].Type);
        Assert.IsType<NullTypeDefinition>(union.Elements[1].Type);
    }

    [Fact]
    public void DiscriminatedInitializer_CanWireCaseBackReference()
    {
        var type = new DiscriminatedObjectTypeDefinition("Shape", "kind", self =>
        {
            var branch = new ObjectTypeDefinition
            {
                Name = "Branch",
                Properties =
                [
                    new() { Name = "kind", Type = new StringLiteralTypeDefinition { Value = "branch" } },
                    new() { Name = "parent", Type = self },
                ],
            };
            return ([], ImmutableDictionary<string, ObjectTypeDefinition>.Empty.Add("branch", branch));
        });

        Assert.Same(type, type.Cases["branch"].Properties[1].Type);
    }

    [Fact]
    public void Initializers_RejectNullCallbacks()
    {
        Assert.Throws<ArgumentNullException>(() => new ArrayTypeDefinition(null!));
        Assert.Throws<ArgumentNullException>(() => new ObjectTypeDefinition("Object", null!));
        Assert.Throws<ArgumentNullException>(() => new UnionTypeDefinition(null!));
        Assert.Throws<ArgumentNullException>(() => new DiscriminatedObjectTypeDefinition("Object", "kind", null!));
    }

    [Fact]
    public void Initializers_RejectUninitializedRequiredEdges()
    {
        Assert.Throws<ArgumentException>(() => new ArrayTypeDefinition(_ => null!));
        Assert.Throws<ArgumentException>(() => new ObjectTypeDefinition("Object", _ => (default, null)));
        Assert.Throws<ArgumentException>(() => new UnionTypeDefinition(_ => default));
        Assert.Throws<ArgumentException>(() => new DiscriminatedObjectTypeDefinition(
            "Object", "kind", _ => ([], null!)));
        Assert.Throws<ArgumentException>(() => new DiscriminatedObjectTypeDefinition(
            "Object", "kind", _ => (default, ImmutableDictionary<string, ObjectTypeDefinition>.Empty)));
    }

    [Fact]
    public void InitializerFailures_AreNotReplacedWithFallbackTypes()
    {
        var error = new InvalidOperationException("Cannot construct the child type.");
        var actual = Assert.Throws<InvalidOperationException>(() =>
            new ObjectTypeDefinition("Object", _ => throw error));

        Assert.Same(error, actual);
    }

    [Fact]
    public void SemanticComparison_HandlesCyclesAndStillChecksLaterProperties()
    {
        var first = CreateNode(255);
        var equivalent = CreateNode(255);
        var different = CreateNode(256);

        Assert.True(SemanticModelComparer.AreEqual(first, equivalent));
        Assert.False(SemanticModelComparer.AreEqual(first, different));
    }

    [Fact]
    public void SemanticComparison_HandlesArrayAndUnionCycles()
    {
        var first = new ArrayTypeDefinition(self => self);
        var second = new ArrayTypeDefinition(self => self);
        var different = new ArrayTypeDefinition(self => self) { MaxLength = 10 };
        var firstUnion = new UnionTypeDefinition(self => [new() { Type = self }]);
        var secondUnion = new UnionTypeDefinition(self => [new() { Type = self }]);

        Assert.True(SemanticModelComparer.AreEqual(first, second));
        Assert.False(SemanticModelComparer.AreEqual(first, different));
        Assert.True(SemanticModelComparer.AreEqual(firstUnion, secondUnion));
    }

    [Fact]
    public void SemanticComparison_ChecksDiscriminatorCaseKeysAndContents()
    {
        var branch = new ObjectTypeDefinition { Name = "Branch", Properties = [] };
        var first = new DiscriminatedObjectTypeDefinition
        {
            Name = "Shape",
            Discriminator = "kind",
            BaseProperties = [],
            Cases = ImmutableDictionary<string, ObjectTypeDefinition>.Empty.Add("one", branch),
        };
        var equivalent = first with
        {
            Cases = ImmutableDictionary<string, ObjectTypeDefinition>.Empty.Add("one", branch with { }),
        };
        var changedKey = first with
        {
            Cases = ImmutableDictionary<string, ObjectTypeDefinition>.Empty.Add("two", branch),
        };
        var changedValue = first with
        {
            Cases = first.Cases.SetItem("one", branch with { Sensitive = true }),
        };

        Assert.True(SemanticModelComparer.AreEqual(first, equivalent));
        Assert.False(SemanticModelComparer.AreEqual(first, changedKey));
        Assert.False(SemanticModelComparer.AreEqual(first, changedValue));
    }

    private static ObjectTypeDefinition CreateNode(long maxValue)
        => new("Node", self =>
        ([
            new PropertyDefinition { Name = "next", Type = self },
            new PropertyDefinition { Name = "value", Type = new IntegerTypeDefinition { MinValue = 0, MaxValue = maxValue } },
        ], null));
}
