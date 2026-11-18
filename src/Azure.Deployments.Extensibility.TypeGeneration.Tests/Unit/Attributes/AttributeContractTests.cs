// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Reflection;
using Azure.Bicep.Types.Concrete;
using Azure.Deployments.Extensibility.TypeGeneration.Attributes;
using Xunit;

namespace Azure.Deployments.Extensibility.TypeGeneration.Tests.Unit.Attributes;

[Trait("Category", "Unit")]
public class AttributeContractTests
{
    [Theory]
    [InlineData(null, "Repository")]
    [InlineData("", "Repository@")]
    [InlineData("2026-08-01", "Repository@2026-08-01")]
    public void ResourceType_PreservesLegacyVersionAndFullName(string? version, string fullName)
    {
        var attribute = new ResourceTypeAttribute("Repository", version);

        Assert.Equal("Repository", attribute.Name);
        Assert.Equal(version, attribute.ApiVersion);
        Assert.Equal(fullName, attribute.FullName);
        Assert.Empty(attribute.ApiVersions);
    }

    [Fact]
    public void ResourceType_OmittedVersionRemainsUnversioned()
    {
        var attribute = new ResourceTypeAttribute("Repository");

        Assert.Null(attribute.ApiVersion);
        Assert.Empty(attribute.ApiVersions);
        Assert.Equal("Repository", attribute.FullName);
    }

    [Fact]
    public void ResourceType_RejectsNullName()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => new ResourceTypeAttribute(null!));

        Assert.Equal("name", exception.ParamName);
    }

    [Fact]
    public void ResourceType_PreservesVersionArrayOrderWithoutChangingLegacyFullName()
    {
        var attribute = new ResourceTypeAttribute("Repository")
        {
            ApiVersions = ["v2", "v1"],
        };

        Assert.Equal(["v2", "v1"], attribute.ApiVersions);
        Assert.Null(attribute.ApiVersion);
        Assert.Equal("Repository", attribute.FullName);
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("Description", "Description")]
    public void TypeProperty_PreservesDescriptionDefaults(string? input, string expected)
    {
        var attribute = new TypePropertyAttribute(input);

        Assert.Equal(expected, attribute.Description);
        Assert.Equal(ObjectTypePropertyFlags.None, attribute.Flags);
        Assert.False(attribute.IsSecure);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TypeProperty_PreservesFlagsAndSecureSetting(bool isSecure)
    {
        var flags = ObjectTypePropertyFlags.Required | ObjectTypePropertyFlags.Identifier;
        var attribute = new TypePropertyAttribute("Name", flags, isSecure);

        Assert.Equal("Name", attribute.Description);
        Assert.Equal(flags, attribute.Flags);
        Assert.Equal(isSecure, attribute.IsSecure);
    }

    [Theory]
    [InlineData(ObjectTypePropertyFlags.None, 0)]
    [InlineData(ObjectTypePropertyFlags.Required, 1)]
    [InlineData(ObjectTypePropertyFlags.ReadOnly, 2)]
    [InlineData(ObjectTypePropertyFlags.WriteOnly, 4)]
    [InlineData(ObjectTypePropertyFlags.DeployTimeConstant, 8)]
    [InlineData(ObjectTypePropertyFlags.Identifier, 16)]
    public void TypeProperty_FlagValuesRemainCompatible(ObjectTypePropertyFlags flag, int value)
    {
        Assert.Equal(value, (int)flag);
        Assert.Equal(flag, new TypePropertyAttribute(null, flag).Flags);
    }

    [Theory]
    [InlineData(typeof(ResourceTypeAttribute), AttributeTargets.Class, true)]
    [InlineData(typeof(TypePropertyAttribute), AttributeTargets.Property, true)]
    [InlineData(typeof(BicepConfigurationAttribute), AttributeTargets.Class, false)]
    public void Attributes_HaveExpectedTargetsAndInheritance(
        Type attributeType,
        AttributeTargets targets,
        bool inherited)
    {
        var usage = Assert.IsType<AttributeUsageAttribute>(
            attributeType.GetCustomAttribute<AttributeUsageAttribute>());

        Assert.Equal(targets, usage.ValidOn);
        Assert.Equal(inherited, usage.Inherited);
        Assert.False(usage.AllowMultiple);
    }

    [Theory]
    [InlineData(typeof(ResourceTypeAttribute))]
    [InlineData(typeof(TypePropertyAttribute))]
    [InlineData(typeof(BicepConfigurationAttribute))]
    public void Attributes_BelongToTheNewSdk(Type attributeType)
    {
        Assert.Equal(
            "Azure.Deployments.Extensibility.TypeGeneration.Sdk",
            attributeType.Assembly.GetName().Name);
        Assert.Equal(
            "Azure.Deployments.Extensibility.TypeGeneration.Attributes",
            attributeType.Namespace);
    }

    [Fact]
    public void Flags_StillComeFromTheTypesPackage()
    {
        Assert.Equal("Azure.Bicep.Types", typeof(ObjectTypePropertyFlags).Assembly.GetName().Name);
        Assert.Equal(
            "Azure.Bicep.Types.Concrete.ObjectTypePropertyFlags",
            typeof(ObjectTypePropertyFlags).FullName);
    }

    [Fact]
    public void SdkAndTests_DoNotReferenceTheOldLocalExtensionSdk()
    {
        foreach (var assembly in new[] { typeof(ResourceTypeAttribute).Assembly, GetType().Assembly })
        {
            Assert.DoesNotContain(
                assembly.GetReferencedAssemblies(),
                reference => reference.Name is "Azure.Bicep.Local.Extension" or "Azure.Bicep.Local.Rpc"
                    or "Bicep.Local.Extension" or "Bicep.Local.Rpc" or "Bicep.Core");
        }
    }

    [Fact]
    public void ConfigurationMarker_IsNotInherited()
    {
        Assert.NotNull(typeof(ConfigurationBase).GetCustomAttribute<BicepConfigurationAttribute>(inherit: true));
        Assert.Null(typeof(DerivedConfiguration).GetCustomAttribute<BicepConfigurationAttribute>(inherit: true));
    }

    [Fact]
    public void ResourceMarker_RemainsInherited()
    {
        var attribute = Assert.IsType<ResourceTypeAttribute>(
            typeof(DerivedResource).GetCustomAttribute<ResourceTypeAttribute>(inherit: true));

        Assert.Equal("BaseResource", attribute.Name);
    }

    [BicepConfiguration]
    private class ConfigurationBase;

    private sealed class DerivedConfiguration : ConfigurationBase;

    [ResourceType("BaseResource")]
    private class ResourceBase;

    private sealed class DerivedResource : ResourceBase;
}
