// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Text;
using Azure.Deployments.Extensibility.TypeGeneration.Semantics;
using Azure.Deployments.Extensibility.TypeGeneration.Tests.Support;
using Xunit;

namespace Azure.Deployments.Extensibility.TypeGeneration.Tests.Unit.Semantics;

[Trait("Category", "Unit")]
public class ModelConstructionTests
{
    [Fact]
    public void ContosoModel_ContainsOneResourceWithOneBodyAndOrderedVersions()
    {
        var model = SemanticModelFixtures.CreateContoso();
        var resource = Assert.Single(model.ResourceTypes);
        var body = Assert.IsType<ObjectTypeDefinition>(resource.Body);
        var configuration = Assert.IsType<ObjectTypeDefinition>(model.Configuration);

        Assert.Equal("Contoso", model.Name);
        Assert.Equal("1.2", model.Version);
        Assert.False(model.IsSingleton);
        Assert.False(model.IsPreview);
        Assert.False(model.IsDeprecated);
        Assert.Null(model.FallbackResourceType);
        Assert.Null(model.SourceLocation);
        Assert.Equal("ContosoConfiguration", configuration.Name);
        Assert.Equal("organization", Assert.Single(configuration.Properties).Name);
        Assert.Equal("Repository", resource.Name);
        Assert.Equal<string>(["2026-08-01", "2026-09-01"], resource.ApiVersions);
        Assert.Equal(ResourceScope.All, resource.ReadableScopes);
        Assert.Equal(ResourceScope.All, resource.WritableScopes);
        Assert.Empty(resource.ResourceFunctions);
        Assert.Equal(["name", "description", "url"], body.Properties.Select(property => property.Name));
        Assert.Equal(PropertyFlags.Required | PropertyFlags.Identifier, body.Properties[0].Flags);
        Assert.Equal(PropertyFlags.ReadOnly, body.Properties[2].Flags);
        Assert.IsType<StringTypeDefinition>(body.Properties[0].Type);
        Assert.IsType<UnionTypeDefinition>(body.Properties[1].Type);
    }

    [Fact]
    public void Registration_PreservesLabelsWithoutSortingOrSynthesizingVersions()
    {
        var resource = Assert.Single(SemanticModelFixtures.CreateContoso().ResourceTypes);
        var ordered = resource with { ApiVersions = ["d2", "d1", "d10"] };
        var unversioned = resource with { ApiVersions = [] };

        Assert.Equal<string>(["d2", "d1", "d10"], ordered.ApiVersions);
        Assert.Empty(unversioned.ApiVersions);
        Assert.Same(resource.Body, ordered.Body);
        Assert.Same(resource.Body, unversioned.Body);
    }

    [Fact]
    public void RequiredAndNullable_AreIndependent()
    {
        var nullableText = new UnionTypeDefinition
        {
            Elements =
            [
                new() { Type = new StringTypeDefinition() },
                new() { Type = new NullTypeDefinition() },
            ],
        };
        var property = new PropertyDefinition
        {
            Name = "name",
            Type = nullableText,
            Flags = PropertyFlags.Required,
        };
        var optional = property with { Flags = PropertyFlags.None };

        Assert.Equal(PropertyFlags.Required, property.Flags);
        Assert.Same(property.Type, optional.Type);
        Assert.IsType<StringTypeDefinition>(nullableText.Elements[0].Type);
        Assert.IsType<NullTypeDefinition>(nullableText.Elements[1].Type);
    }

    [Fact]
    public void CollectionsAndEnums_UseOnlyNormalizedTypeNodes()
    {
        var literals = new UnionTypeDefinition
        {
            Elements =
            [
                new() { Type = new StringLiteralTypeDefinition { Value = "First" } },
                new() { Type = new StringLiteralTypeDefinition { Value = "Second" } },
            ],
        };
        var dictionary = new ObjectTypeDefinition
        {
            Name = "Settings",
            Properties = [],
            AdditionalProperties = literals,
        };
        var array = new ArrayTypeDefinition { ItemType = dictionary };

        Assert.Same(dictionary, array.ItemType);
        Assert.Empty(dictionary.Properties);
        Assert.Same(literals, dictionary.AdditionalProperties);
        Assert.Equal(["First", "Second"], literals.Elements.Select(
            element => Assert.IsType<StringLiteralTypeDefinition>(element.Type).Value));
    }

    [Fact]
    public void ConstraintFields_PreserveSuppliedValuesAndHaveEmptyDefaults()
    {
        var integer = new IntegerTypeDefinition { MinValue = 0, MaxValue = 255 };
        var text = new StringTypeDefinition
        {
            Sensitive = true,
            MinLength = 3,
            MaxLength = 100,
            Pattern = "[a-z]+",
        };
        var array = new ArrayTypeDefinition { ItemType = text, MinLength = 1, MaxLength = 10 };
        var sensitiveObject = new ObjectTypeDefinition { Name = "Secret", Properties = [], Sensitive = true };

        Assert.Equal(0, integer.MinValue);
        Assert.Equal(255, integer.MaxValue);
        Assert.True(text.Sensitive);
        Assert.Equal(3, text.MinLength);
        Assert.Equal(100, text.MaxLength);
        Assert.Equal("[a-z]+", text.Pattern);
        Assert.Equal(1, array.MinLength);
        Assert.Equal(10, array.MaxLength);
        Assert.True(sensitiveObject.Sensitive);
        Assert.Null(new IntegerTypeDefinition().MinValue);
        Assert.Null(new IntegerTypeDefinition().MaxValue);
        var plainText = new StringTypeDefinition();
        Assert.False(plainText.Sensitive);
        Assert.Null(plainText.MinLength);
        Assert.Null(plainText.MaxLength);
        Assert.Null(plainText.Pattern);
        var plainArray = new ArrayTypeDefinition { ItemType = plainText };
        Assert.Null(plainArray.MinLength);
        Assert.Null(plainArray.MaxLength);
    }

    [Fact]
    public void VersionMetadata_DefaultsToEmptyWithoutSharingPropertyState()
    {
        var property = new PropertyDefinition { Name = "one", Type = new BooleanTypeDefinition() };
        var other = new PropertyDefinition { Name = "two", Type = new BooleanTypeDefinition() };
        var element = new UnionElementDefinition { Type = new StringLiteralTypeDefinition { Value = "Basic" } };
        var function = new ResourceFunctionDefinition { Name = "read", OutputType = new AnyTypeDefinition() };

        Assert.Equal(PropertyFlags.None, property.Flags);
        Assert.Null(property.Description);
        Assert.Null(property.Versioning.Availability.AddedAt);
        Assert.Null(property.Versioning.Availability.RemovedAt);
        Assert.Null(property.Versioning.NameChangedFrom);
        Assert.Null(property.Versioning.TypeChangedFrom);
        Assert.Null(property.Versioning.RequiredChangedFrom);
        Assert.NotSame(property.Versioning, other.Versioning);
        Assert.NotSame(property.Versioning.Availability, other.Versioning.Availability);
        Assert.Null(element.Availability.AddedAt);
        Assert.Null(element.Availability.RemovedAt);
        Assert.Null(function.InputType);
        Assert.Null(function.Availability.AddedAt);
        Assert.Null(function.Availability.RemovedAt);
    }

    [Fact]
    public void VersionMetadata_StoresHistoryWithoutApplyingIt()
    {
        var oldType = new StringTypeDefinition();
        var currentType = new IntegerTypeDefinition();
        var property = new PropertyDefinition
        {
            Name = "newName",
            Type = currentType,
            Versioning = new PropertyVersioning
            {
                Availability = new() { AddedAt = "v2", RemovedAt = "v4" },
                NameChangedFrom = new() { AtVersion = "v3", PreviousValue = "oldName" },
                TypeChangedFrom = new() { AtVersion = "v3", PreviousValue = oldType },
                RequiredChangedFrom = new() { AtVersion = "v2", PreviousValue = true },
            },
        };

        Assert.Equal("newName", property.Name);
        Assert.Same(currentType, property.Type);
        Assert.Equal(PropertyFlags.None, property.Flags);
        Assert.Equal("v2", property.Versioning.Availability.AddedAt);
        Assert.Equal("v4", property.Versioning.Availability.RemovedAt);
        Assert.Equal("v3", property.Versioning.NameChangedFrom.AtVersion);
        Assert.Equal("oldName", property.Versioning.NameChangedFrom.PreviousValue);
        Assert.Same(oldType, property.Versioning.TypeChangedFrom.PreviousValue);
        Assert.True(property.Versioning.RequiredChangedFrom.PreviousValue);
    }

    [Fact]
    public void DiscriminatedObjects_StoreBasePropertiesAndNamedCases()
    {
        var shape = new ObjectTypeDefinition
        {
            Name = "Circle",
            Properties =
            [
                new() { Name = "kind", Type = new StringLiteralTypeDefinition { Value = "circle" } },
            ],
        };
        var type = new DiscriminatedObjectTypeDefinition
        {
            Name = "Shape",
            Discriminator = "kind",
            BaseProperties =
            [
                new() { Name = "name", Type = new StringTypeDefinition(), Flags = PropertyFlags.Required },
            ],
            Cases = ImmutableDictionary<string, ObjectTypeDefinition>.Empty.Add("circle", shape),
        };

        Assert.Equal("kind", type.Discriminator);
        Assert.Equal("name", Assert.Single(type.BaseProperties).Name);
        Assert.Same(shape, type.Cases["circle"]);
    }

    [Fact]
    public void FunctionAndProjectedRecords_ReuseTheSemanticTypeHierarchy()
    {
        var body = new ObjectTypeDefinition { Name = "Repository", Properties = [] };
        var output = new ObjectTypeDefinition { Name = "Keys", Properties = [] };
        var location = new SourceLocation { FilePath = "Repository.cs", Start = 12, Length = 5 };
        var function = new ResourceFunctionDefinition
        {
            Name = "listKeys",
            OutputType = output,
            Description = "Read keys",
            Availability = new() { AddedAt = "v2" },
            SourceLocation = location,
        };
        var projectedFunction = new ProjectedResourceFunctionDefinition
        {
            Name = function.Name,
            OutputType = output,
            Description = function.Description,
            SourceLocation = location,
        };
        var projected = new ProjectedResourceTypeDefinition
        {
            Name = "Repository",
            ApiVersion = "v2",
            Body = body,
            ReadableScopes = ResourceScope.All,
            WritableScopes = ResourceScope.All,
            ResourceFunctions = [projectedFunction],
            SourceLocation = location,
        };
        var unversioned = projected with { ApiVersion = null, ResourceFunctions = [] };

        Assert.Null(function.InputType);
        Assert.Null(projectedFunction.InputType);
        Assert.Same(output, projectedFunction.OutputType);
        Assert.Same(body, projected.Body);
        Assert.Same(location, projected.SourceLocation);
        Assert.Equal("v2", projected.ApiVersion);
        Assert.Null(unversioned.ApiVersion);
        Assert.Empty(unversioned.ResourceFunctions);
    }

    [Fact]
    public void SourceLocation_PreservesUtf16SpanAndFileIdentity()
    {
        const string source = "// \U0001F9EA\nName";
        var location = new SourceLocation
        {
            FilePath = @"C:\fixtures\Model.cs",
            Start = source.IndexOf("Name", StringComparison.Ordinal),
            Length = "Name".Length,
        };

        Assert.Equal(6, location.Start);
        Assert.Equal("Name", source.Substring(location.Start, location.Length));
        Assert.NotEqual(Encoding.UTF8.GetByteCount(source.AsSpan(0, location.Start)), location.Start);
        Assert.Equal(@"C:\fixtures\Model.cs", location.FilePath);
    }

    [Fact]
    public void SemanticComparison_IgnoresLocationsThroughoutTheModel()
    {
        var first = SemanticModelFixtures.CreateContoso(
            new SourceLocation { FilePath = "first.cs", Start = 10, Length = 4 });
        var second = SemanticModelFixtures.CreateContoso(
            new SourceLocation { FilePath = "second.cs", Start = 20, Length = 8 });

        Assert.True(SemanticModelComparer.AreEqual(first, second));
        Assert.False(SemanticModelComparer.AreEqual(first, second with { Version = "2.0" }));
        Assert.Equal("first.cs", first.SourceLocation!.FilePath);
        Assert.Equal("second.cs", second.SourceLocation!.FilePath);
    }

    [Theory]
    [InlineData("name")]
    [InlineData("versions")]
    [InlineData("scopes")]
    [InlineData("flags")]
    [InlineData("description")]
    [InlineData("sensitive")]
    [InlineData("history")]
    public void SemanticComparison_DetectsChangedValues(string field)
    {
        var expected = SemanticModelFixtures.CreateContoso();
        var resource = Assert.Single(expected.ResourceTypes);
        var body = Assert.IsType<ObjectTypeDefinition>(resource.Body);
        var property = body.Properties[0];
        var changedProperty = field switch
        {
            "flags" => property with { Flags = PropertyFlags.None },
            "description" => property with { Description = "Changed" },
            "sensitive" => property with { Type = new StringTypeDefinition { Sensitive = true } },
            "history" => property with
            {
                Versioning = new() { NameChangedFrom = new() { AtVersion = "2026-09-01", PreviousValue = "old" } },
            },
            _ => property,
        };
        var changedResource = field switch
        {
            "name" => resource with { Name = "Other" },
            "versions" => resource with { ApiVersions = ["2026-09-01", "2026-08-01"] },
            "scopes" => resource with { WritableScopes = ResourceScope.None },
            _ => resource with { Body = body with { Properties = body.Properties.SetItem(0, changedProperty) } },
        };

        Assert.False(SemanticModelComparer.AreEqual(
            expected,
            expected with { ResourceTypes = [changedResource] }));
    }
}
