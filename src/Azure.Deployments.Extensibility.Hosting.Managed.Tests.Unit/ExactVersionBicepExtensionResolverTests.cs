// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore;
using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using Azure.Deployments.Extensibility.Hosting.Managed;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Azure.Deployments.Extensibility.Hosting.Managed.Tests.Unit;

public class ExactVersionBicepExtensionResolverTests
{
    [Fact]
    public void Resolve_WithExactVersionMatch_ReturnsRegistration()
    {
        var services = new ServiceCollection();
        var registration = BicepExtensionRegistration.Create(services, ext => ext.AddHandler<StubGetHandler>());
        var resolver = new ExactVersionBicepExtensionResolver("1.0.0", registration);

        var resolved = resolver.Resolve("1.0.0");

        resolved.Should().BeSameAs(registration);
    }

    [Theory]
    [InlineData("1.0.1")]
    [InlineData("1.0.0-preview")]
    [InlineData("2.0.0")]
    [InlineData("1.*.*")]
    [InlineData(">=1.0.0 <2.0.0")]
    [InlineData("1.0")]
    [InlineData("v1.0.0")]
    public void Resolve_WithMismatchedVersion_ReturnsNull(string incomingVersion)
    {
        var services = new ServiceCollection();
        var registration = BicepExtensionRegistration.Create(services, ext => ext.AddHandler<StubGetHandler>());
        var resolver = new ExactVersionBicepExtensionResolver("1.0.0", registration);

        var resolved = resolver.Resolve(incomingVersion);

        resolved.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithNullOrEmptyVersion_Throws()
    {
        var services = new ServiceCollection();
        var registration = BicepExtensionRegistration.Create(services, ext => ext.AddHandler<StubGetHandler>());

        var actionNull = () => new ExactVersionBicepExtensionResolver(null!, registration);
        var actionEmpty = () => new ExactVersionBicepExtensionResolver("", registration);

        actionNull.Should().Throw<ArgumentException>();
        actionEmpty.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithNullRegistration_Throws()
    {
        var action = () => new ExactVersionBicepExtensionResolver("1.0.0", null!);

        action.Should().Throw<ArgumentNullException>();
    }

    private sealed class StubGetHandler : IResourceGetHandler
    {
        public Task<OneOf<Resource?, ErrorResponse>> HandleAsync(
            ResourceReference request,
            CancellationToken cancellationToken) =>
            Task.FromResult<OneOf<Resource?, ErrorResponse>>((Resource?)null);
    }
}
