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
    /// (and the resource preview Status) when mapping between strongly-typed handler models and the
    /// untyped contract models, in both directions. These fields back the RELO (resource-based
    /// long-running operation) flow, so dropping them silently breaks asynchronous status polling.
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
        public async Task CreateOrUpdate_NonTerminalStatus_IsPropagatedToResource()
        {
            var sut = new StubCreateOrUpdateHandler(JsonOptions, status: "Running");

            var result = await ((IResourceCreateOrUpdateHandler)sut).HandleAsync(CreateSpecification(), CancellationToken.None);

            result.IsT0.Should().BeTrue();
            result.AsT0!.Status.Should().Be("Running");
            result.AsT0!.Error.Should().BeNull();
        }

        [Fact]
        public async Task CreateOrUpdate_FailedStatusAndError_ArePropagatedToResource()
        {
            var sut = new StubCreateOrUpdateHandler(JsonOptions, status: "Failed", error: SimulatedError());

            var result = await ((IResourceCreateOrUpdateHandler)sut).HandleAsync(CreateSpecification(), CancellationToken.None);

            result.IsT0.Should().BeTrue();
            result.AsT0!.Status.Should().Be("Failed");
            result.AsT0!.Error!.Code.Should().Be("Simulated");
        }

        [Fact]
        public async Task Get_Status_IsPropagatedToResource()
        {
            var sut = new StubGetHandler(JsonOptions, status: "Succeeded");

            var result = await ((IResourceGetHandler)sut).HandleAsync(CreateReference(), CancellationToken.None);

            result.IsT0.Should().BeTrue();
            result.AsT0!.Status.Should().Be("Succeeded");
        }

        [Fact]
        public async Task Delete_NonTerminalStatus_IsPropagatedToResource()
        {
            var sut = new StubDeleteHandler(JsonOptions, status: "Deleting");

            var result = await ((IResourceDeleteHandler)sut).HandleAsync(CreateReference(), CancellationToken.None);

            result.IsT0.Should().BeTrue();
            result.AsT0!.Status.Should().Be("Deleting");
            result.AsT0!.Error.Should().BeNull();
        }

        [Fact]
        public async Task Preview_Status_IsPropagatedToResourcePreview()
        {
            var sut = new StubPreviewHandler(JsonOptions, status: "Running");

            var request = new ResourcePreviewSpecification
            {
                Type = "Test",
                Properties = new JsonObject { ["name"] = "widget" },
                Metadata = new ResourcePreviewSpecificationMetadata()
            };

            var result = await ((IResourcePreviewHandler)sut).HandleAsync(request, CancellationToken.None);

            result.IsT0.Should().BeTrue();
            result.AsT0!.Status.Should().Be("Running");
        }

        [Fact]
        public void ToTypedResource_FailedStatusAndError_ArePropagated()
        {
            var probe = new MappingProbe(JsonOptions);
            var resource = new Resource
            {
                Type = "Test",
                Identifiers = new JsonObject { ["name"] = "widget" },
                Properties = new JsonObject { ["name"] = "widget" },
                Status = "Failed",
                Error = SimulatedError(),
            };

            var (status, error) = probe.ConvertToTyped(resource);

            status.Should().Be("Failed");
            error!.Code.Should().Be("Simulated");
        }

        [Fact]
        public void ToTypedResourcePreview_Status_IsPropagated()
        {
            var probe = new MappingProbe(JsonOptions);
            var preview = new ResourcePreview
            {
                Type = "Test",
                Identifiers = new JsonObject { ["name"] = "widget" },
                Properties = new JsonObject { ["name"] = "widget" },
                Status = "Running",
            };

            probe.ConvertToTypedPreview(preview).Should().Be("Running");
        }

        private static Error SimulatedError() => new() { Code = "Simulated", Message = "Simulated failure." };

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
            private readonly string status;
            private readonly Error? error;

            public StubCreateOrUpdateHandler(IOptions<JsonOptions> jsonOptions, string status, Error? error = null)
                : base(jsonOptions)
            {
                this.status = status;
                this.error = error;
            }

            protected override Task<OneOf<TypedResource, LongRunningOperation, ErrorResponse>> HandleAsync(TypedResourceSpecification request, CancellationToken cancellationToken) =>
                Task.FromResult<OneOf<TypedResource, LongRunningOperation, ErrorResponse>>(new TypedResource
                {
                    Type = request.Type,
                    Identifiers = new TestIdentifiers { Name = "widget" },
                    Properties = request.Properties,
                    Status = this.status,
                    Error = this.error,
                });
        }

        private sealed class StubGetHandler : TypedResourceGetHandler<TestProperties, TestIdentifiers>
        {
            private readonly string status;

            public StubGetHandler(IOptions<JsonOptions> jsonOptions, string status)
                : base(jsonOptions)
            {
                this.status = status;
            }

            protected override Task<OneOf<TypedResource?, ErrorResponse>> HandleAsync(TypedResourceReference request, CancellationToken cancellationToken) =>
                Task.FromResult<OneOf<TypedResource?, ErrorResponse>>(new TypedResource
                {
                    Type = request.Type,
                    Identifiers = new TestIdentifiers { Name = "widget" },
                    Properties = new TestProperties { Name = "widget" },
                    Status = this.status,
                });
        }

        private sealed class StubDeleteHandler : TypedResourceDeleteHandler<TestProperties, TestIdentifiers>
        {
            private readonly string status;

            public StubDeleteHandler(IOptions<JsonOptions> jsonOptions, string status)
                : base(jsonOptions)
            {
                this.status = status;
            }

            protected override Task<OneOf<TypedResource?, LongRunningOperation, ErrorResponse>> HandleAsync(TypedResourceReference request, CancellationToken cancellationToken) =>
                Task.FromResult<OneOf<TypedResource?, LongRunningOperation, ErrorResponse>>(new TypedResource
                {
                    Type = request.Type,
                    Identifiers = new TestIdentifiers { Name = "widget" },
                    Properties = new TestProperties { Name = "widget" },
                    Status = this.status,
                });
        }

        private sealed class StubPreviewHandler : TypedResourcePreviewHandler<TestProperties, TestIdentifiers>
        {
            private readonly string status;

            public StubPreviewHandler(IOptions<JsonOptions> jsonOptions, string status)
                : base(jsonOptions)
            {
                this.status = status;
            }

            protected override Task<OneOf<TypedResourcePreview, ErrorResponse>> HandleAsync(TypedResourcePreviewSpecification request, CancellationToken cancellationToken) =>
                Task.FromResult<OneOf<TypedResourcePreview, ErrorResponse>>(new TypedResourcePreview
                {
                    Type = request.Type,
                    Identifiers = new TestIdentifiers { Name = "widget" },
                    Properties = request.Properties,
                    Status = this.status,
                });
        }

        /// <summary>
        /// Exposes the protected untyped-to-typed mapping helpers for direct testing.
        /// </summary>
        private sealed class MappingProbe : TypedResourceOperationHandler<TestProperties, TestIdentifiers, JsonObject?>
        {
            public MappingProbe(IOptions<JsonOptions> jsonOptions)
                : base(jsonOptions)
            {
            }

            public (string? Status, Error? Error) ConvertToTyped(Resource resource)
            {
                var typed = this.ToTypedResource(resource);

                return (typed.Status, typed.Error);
            }

            public string? ConvertToTypedPreview(ResourcePreview preview) => this.ToTypedResourcePreview(preview).Status;
        }
    }
}
