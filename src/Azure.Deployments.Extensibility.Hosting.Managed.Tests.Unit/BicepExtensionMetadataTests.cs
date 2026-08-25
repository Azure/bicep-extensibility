// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Hosting.Managed;
using FluentAssertions;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace Azure.Deployments.Extensibility.Hosting.Managed.Tests.Unit;

public class BicepExtensionMetadataTests
{
    [Fact]
    public void Constructor_WithValidArguments_InitializesProperties()
    {
        var metadata = new BicepExtensionMetadata("TestExtension", "1.2.3");

        metadata.Name.Should().Be("TestExtension");
        metadata.Version.Should().Be("1.2.3");
    }

    [Theory]
    [InlineData(null, "1.0.0")]
    [InlineData("", "1.0.0")]
    [InlineData("   ", "1.0.0")]
    [InlineData("TestExtension", null)]
    [InlineData("TestExtension", "")]
    [InlineData("TestExtension", "   ")]
    public void Constructor_WithInvalidArguments_Throws(string? name, string? version)
    {
        var action = () => new BicepExtensionMetadata(name!, version!);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void FromAssembly_WithMetadataAttributes_ReturnsMetadata()
    {
        var assembly = CreateDynamicAssembly(
            new Dictionary<string, string>
            {
                [BicepExtensionMetadata.ExtensionNameMetadataKey] = "DynamicExtension",
                [BicepExtensionMetadata.ExtensionVersionMetadataKey] = "2.0.0-beta.1",
            });

        var metadata = BicepExtensionMetadata.FromAssembly(assembly);

        metadata.Name.Should().Be("DynamicExtension");
        metadata.Version.Should().Be("2.0.0-beta.1");
    }

    [Fact]
    public void FromAssembly_WithLegacyMetadataAttributes_ReturnsMetadata()
    {
        var assembly = CreateDynamicAssembly(
            new Dictionary<string, string>
            {
                [BicepExtensionMetadata.LegacyExtensionNameMetadataKey] = "LegacyExtension",
                [BicepExtensionMetadata.LegacyExtensionVersionMetadataKey] = "1.0.0",
            });

        var metadata = BicepExtensionMetadata.FromAssembly(assembly);

        metadata.Name.Should().Be("LegacyExtension");
        metadata.Version.Should().Be("1.0.0");
    }

    [Fact]
    public void FromAssembly_WithoutMetadataAttributes_Throws()
    {
        var assembly = CreateDynamicAssembly(new Dictionary<string, string>());

        var action = () => BicepExtensionMetadata.FromAssembly(assembly);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*Bicep extension identity metadata is missing*");
    }

    [Fact]
    public void FromAssembly_WithMissingVersion_Throws()
    {
        var assembly = CreateDynamicAssembly(
            new Dictionary<string, string>
            {
                [BicepExtensionMetadata.ExtensionNameMetadataKey] = "TestExtension",
            });

        var action = () => BicepExtensionMetadata.FromAssembly(assembly);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*Bicep extension identity metadata is missing*");
    }

    private static Assembly CreateDynamicAssembly(IDictionary<string, string> metadata)
    {
        var assemblyName = new AssemblyName($"TestAssembly_{Guid.NewGuid():N}");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);

        var constructor = typeof(AssemblyMetadataAttribute).GetConstructor([typeof(string), typeof(string)])!;

        foreach (var (key, value) in metadata)
        {
            var customAttributeBuilder = new CustomAttributeBuilder(constructor, [key, value]);
            assemblyBuilder.SetCustomAttribute(customAttributeBuilder);
        }

        return assemblyBuilder;
    }
}
