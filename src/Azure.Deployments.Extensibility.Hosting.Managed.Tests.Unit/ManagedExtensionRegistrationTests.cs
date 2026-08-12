// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore;
using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using Azure.Deployments.Extensibility.Hosting.Managed.Metadata;
using Azure.Deployments.Extensibility.Hosting.Managed.Resolution;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace Azure.Deployments.Extensibility.Hosting.Managed.Tests.Unit;

public class ManagedExtensionRegistrationTests
{
    [Fact]
    public void BicepExtensionDescriptorReader_MissingMetadata_Throws()
    {
        var assembly = CreateAssembly();

        var action = () => BicepExtensionDescriptorReader.Read(assembly);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*Bicep.Extension.Name*");
    }

    [Fact]
    public void BicepExtensionDescriptorReader_BlankMetadata_Throws()
    {
        var assembly = CreateAssembly(
            ("Bicep.Extension.Name", " "),
            ("Bicep.Extension.Version", "1.0.0"));

        var action = () => BicepExtensionDescriptorReader.Read(assembly);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*Bicep.Extension.Name*");
    }

    [Fact]
    public void BicepExtensionDescriptorReader_DuplicateMetadata_Throws()
    {
        var assembly = CreateAssembly(
            ("Bicep.Extension.Name", "Widget"),
            ("Bicep.Extension.Version", "1.0.0"),
            ("Bicep.Extension.Version", "2.0.0"));

        var action = () => BicepExtensionDescriptorReader.Read(assembly);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*Bicep.Extension.Version*");
    }

    [Fact]
    public void AddBicepExtension_CalledTwice_Throws()
    {
        var services = new ServiceCollection();
        var descriptor = new BicepExtensionDescriptor("Widget", "1.0.0");
        services.AddBicepExtension(extension => extension.AddHandler<GetHandler>(), descriptor);

        var action = () => services.AddBicepExtension(
            extension => extension.AddHandler<GetHandler>(),
            descriptor);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("AddBicepExtension can only be called once.");
    }

    [Theory]
    [InlineData("1.0.0", true)]
    [InlineData("1.0.0 ", false)]
    [InlineData("1.0", false)]
    [InlineData("1.0.0+build", false)]
    [InlineData("1.*.*", false)]
    [InlineData("1.0.0-alpha", false)]
    public void ExactVersionExtensionResolver_VersionInput_UsesOrdinalExactMatch(
        string routeVersion,
        bool expectedMatch)
    {
        var services = new ServiceCollection();
        var registration = BicepExtensionRegistration.Create(
            services,
            extension => extension.AddHandler<GetHandler>());
        var sut = new ExactVersionExtensionResolver("1.0.0", registration);

        var result = sut.Resolve(routeVersion);

        (result is not null).Should().Be(expectedMatch);
    }

    [Fact]
    public void ExactVersionExtensionResolver_CasingChange_ReturnsNull()
    {
        var services = new ServiceCollection();
        var registration = BicepExtensionRegistration.Create(
            services,
            extension => extension.AddHandler<GetHandler>());
        var sut = new ExactVersionExtensionResolver("1.0.0-alpha", registration);

        var result = sut.Resolve("1.0.0-ALPHA");

        result.Should().BeNull();
    }

    private static Assembly CreateAssembly(params (string Key, string Value)[] metadata)
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName($"ManagedIdentityTests.{Guid.NewGuid():N}"),
            AssemblyBuilderAccess.Run);
        var constructor = typeof(AssemblyMetadataAttribute).GetConstructor([typeof(string), typeof(string)])!;

        foreach (var (key, value) in metadata)
        {
            assembly.SetCustomAttribute(new CustomAttributeBuilder(constructor, [key, value]));
        }

        return assembly;
    }

    private sealed class GetHandler : IResourceGetHandler
    {
        public Task<OneOf<Resource?, ErrorResponse>> HandleAsync(
            ResourceReference request,
            CancellationToken cancellationToken) =>
            Task.FromResult<OneOf<Resource?, ErrorResponse>>((Resource?)null);
    }
}
