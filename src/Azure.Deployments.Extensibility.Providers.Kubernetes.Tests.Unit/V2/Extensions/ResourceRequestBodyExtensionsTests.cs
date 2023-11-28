// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AutoFixture.Xunit2;
using Azure.Deployments.Extensibility.Core.V2.Models;
using Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Extensions;
using FluentAssertions;
using System.Text.Json.Nodes;
using Xunit;
using static FluentAssertions.FluentActions;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Unit.V2.Extensions
{
    public class ResourceRequestBodyExtensionsTests
    {
        [Theory, AutoData]
        public void GetName_NameExists_ReturnsName(string type, string name)
        {
            var sut = new ResourceRequestBody(type, new JsonObject
            {
                ["metadata"] = new JsonObject
                {
                    ["name"] = name,
                }
            });

            sut.GetName().Should().Be(name);
        }

        [Theory, AutoData]
        public void GetName_NameDoesNotExist_Throws(string type)
        {
            var sut = new ResourceRequestBody(type, []);

            Invoking(sut.GetName).Should().Throw<InvalidOperationException>();
        }

        [Theory, AutoData]
        public void GetName_InvalidType_Throws(string type, bool name)
        {
            var sut = new ResourceRequestBody(type, new JsonObject
            {
                ["metadata"] = new JsonObject
                {
                    ["name"] = name,
                }
            });

            Invoking(sut.GetName).Should().Throw<InvalidOperationException>();
        }

        [Theory, AutoData]
        public void TryGetNamespace_NamespaceExists_ReturnsNamespace(string type, string @namespace)
        {
            var sut = new ResourceRequestBody(type, new JsonObject
            {
                ["metadata"] = new JsonObject
                {
                    ["namespace"] = @namespace,
                }
            });

            sut.TryGetNamespace().Should().Be(@namespace);
        }

        [Theory, AutoData]
        public void TryGetNamespace_NamespaceDoesNotExist_ReturnsNull(string type)
        {
            var sut = new ResourceRequestBody(type, []);

            sut.TryGetNamespace().Should().BeNull();
        }
    }
}
