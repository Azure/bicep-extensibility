// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using System.Text.Json.Nodes;
using Xunit;

namespace Azure.Deployments.Extensibility.AspNetCore.Tests.Unit;

public class ExtensionApplicationTests
{
    [Fact]
    public async Task Build_WithLegacyRegistration_DoesNotRequireNewResolver()
    {
        var sut = ExtensionApplication.Create([]);
        sut.Builder.Environment.EnvironmentName = Environments.Development;
        sut.AddExtensionVersion("1.*.*", version => version.AddHandler<LegacyGetHandler>());

        await using var app = sut.Build();

        app.Should().NotBeNull();
    }

    private sealed class LegacyGetHandler : IResourceGetHandler
    {
        public Task<OneOf<Resource?, ErrorResponse>> HandleAsync(
            ResourceReference request,
            CancellationToken cancellationToken) =>
            Task.FromResult<OneOf<Resource?, ErrorResponse>>((Resource?)null);
    }
}
