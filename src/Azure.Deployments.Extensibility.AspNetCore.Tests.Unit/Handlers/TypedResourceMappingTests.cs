// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using Azure.Deployments.Extensibility.AspNetCore.Handlers;
using Azure.Deployments.Extensibility.Core.V2.Contracts;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Handlers;
using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using Xunit;

namespace Azure.Deployments.Extensibility.AspNetCore.Tests.Unit.Handlers
{
    /// <summary>
    /// Verifies that the typed handler base classes propagate the resource Status and Error fields
    /// (and the resource preview Status) when mapping the strongly-typed result back to the untyped
    /// contract. These fields back the RELO (resource-based long-running operation) flow, so dropping
    /// them silently breaks asynchronous status polling.
    /// </summary>
    public class TypedResourceMappingTests
    {
        private static IOptions<JsonOptions> JsonOptions => Options.Create(new JsonOptions());

        private static ResourceSpecification CreateSpecification() => new()
        {
            Type = "Test",
            Properties = new JsonObject { ["name"] = "widget" },
        };

        private static ResourceReference CreateReference() => new()
        {
            Type = "Test",
            Identifiers = new JsonObject { ["name"] = "widget" },
        };

        [Fact]
        public async Task CreateOrUpdate_NonTerminalStatusAndError_ArePropagatedToResource()
        {
            var sut = new StubCreateOrUpdateHandler(JsonOptions);

            var result = await ((IResourceCreateOrUpdateHandler)sut).HandleAsync(CreateSpecification(), CancellationToken.None);

            result.IsT0.Should().BeTrue();
            result.AsT0!.Status.Should().Be("Running");
            result.AsT0!.Error!.Code.Should().Be("Simulated");
        }

        [Fact]
        public async Task Get_StatusIsPropagatedToResource()
        {
            var sut = new StubGetHandler(JsonOptions);

            var result = await ((IResourceGetHandler)sut).HandleAsync(CreateReference(), CancellationToken.None);

            result.IsT0.Should().BeTrue();
            result.AsT0!.Status.Should().Be("Succeeded");
        }

        [Fact]
        public async Task Delete_StatusAndError_ArePropagatedToResource()
        {
            var sut = new StubDeleteHandler(JsonOptions);

            var result = await ((IResourceDeleteHandler)sut).HandleAsync(CreateReference(), CancellationToken.None);

            result.IsT0.Should().BeTrue();
            result.AsT0!.Status.Should().Be("Deleting");
            result.AsT0!.Error!.Code.Should().Be("Simulated");
        }

        [Fact]
        public async Task Preview_StatusIsPropagatedToResourcePreview()
        {
            var sut = new StubPreviewHandler(JsonOptions);

            var request = new ResourcePreviewSpecification
            {
                Type = "Test",
                Properties = new JsonObject { ["name"] = "widget" },
            };

            var result = await ((IResourcePreviewHandler)sut).HandleAsync(request, CancellationToken.None);

            result.IsT0.Should().BeTrue();
            result.AsT0!.Status.Should().Be("Running");
        }

        public record TestProperties
        {
            public string? Name { get; init; }
        }

        public record TestIdentifiers
        {
            public string? Name { get; init; }
        }

        private sealed class StubCreateOrUpdateHandler : TypedResourceCreateOrUpdateHandler<TestProperties, TestIdentifiers>
        {
            public StubCreateOrUpdateHandler(IOptions<JsonOptions> jsonOptions)
                : base(jsonOptions)
            {
            }

            protected override Task<OneOf<TypedResource, LongRunningOperation, ErrorResponse>> HandleAsync(TypedResourceSpecification request, CancellationToken cancellationToken) =>
                Task.FromResult<OneOf<TypedResource, LongRunningOperation, ErrorResponse>>(new TypedResource
                {
                    Type = request.Type,
                    Identifiers = new TestIdentifiers { Name = "widget" },
                    Properties = request.Properties,
                    Status = "Running",
                    Error = new Error { Code = "Simulated", Message = "Simulated failure." },
                });
        }

        private sealed class StubGetHandler : TypedResourceGetHandler<TestProperties, TestIdentifiers>
        {
            public StubGetHandler(IOptions<JsonOptions> jsonOptions)
                : base(jsonOptions)
            {
            }

            protected override Task<OneOf<TypedResource?, ErrorResponse>> HandleAsync(TypedResourceReference request, CancellationToken cancellationToken) =>
                Task.FromResult<OneOf<TypedResource?, ErrorResponse>>(new TypedResource
                {
                    Type = request.Type,
                    Identifiers = new TestIdentifiers { Name = "widget" },
                    Properties = new TestProperties { Name = "widget" },
                    Status = "Succeeded",
                });
        }

        private sealed class StubDeleteHandler : TypedResourceDeleteHandler<TestProperties, TestIdentifiers>
        {
            public StubDeleteHandler(IOptions<JsonOptions> jsonOptions)
                : base(jsonOptions)
            {
            }

            protected override Task<OneOf<TypedResource?, LongRunningOperation, ErrorResponse>> HandleAsync(TypedResourceReference request, CancellationToken cancellationToken) =>
                Task.FromResult<OneOf<TypedResource?, LongRunningOperation, ErrorResponse>>(new TypedResource
                {
                    Type = request.Type,
                    Identifiers = new TestIdentifiers { Name = "widget" },
                    Properties = new TestProperties { Name = "widget" },
                    Status = "Deleting",
                    Error = new Error { Code = "Simulated", Message = "Simulated failure." },
                });
        }

        private sealed class StubPreviewHandler : TypedResourcePreviewHandler<TestProperties, TestIdentifiers>
        {
            public StubPreviewHandler(IOptions<JsonOptions> jsonOptions)
                : base(jsonOptions)
            {
            }

            protected override Task<OneOf<TypedResourcePreview, ErrorResponse>> HandleAsync(TypedResourcePreviewSpecification request, CancellationToken cancellationToken) =>
                Task.FromResult<OneOf<TypedResourcePreview, ErrorResponse>>(new TypedResourcePreview
                {
                    Type = request.Type,
                    Identifiers = new TestIdentifiers { Name = "widget" },
                    Properties = request.Properties,
                    Status = "Running",
                });
        }
    }
}
