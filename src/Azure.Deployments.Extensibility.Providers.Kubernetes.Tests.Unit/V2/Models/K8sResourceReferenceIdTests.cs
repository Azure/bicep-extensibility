// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AutoFixture.Xunit2;
using Azure.Deployments.Extensibility.Core.V2.Exceptions;
using Azure.Deployments.Extensibility.Core.V2.Models;
using Azure.Deployments.Extensibility.Providers.Kubernetes.V2.Models;
using FluentAssertions;
using k8s.Models;
using System.Text;
using System.Text.Json.Nodes;
using Xunit;
using static FluentAssertions.FluentActions;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Unit.V2.Models
{
    public class K8sResourceReferenceIdTests
    {
        private readonly static JsonObject DummyConfigObject = new()
        {
            ["kubeConfig"] = Convert.ToBase64String(Encoding.UTF8.GetBytes("""
                apiVersion: v1
                clusters:
                - cluster:
                    server: http://example.com
                  name: testcluster
                contexts:
                - context:
                    cluster: testcluster
                    user: testuser
                  name: testcontext
                current-context: testcontext
                kind: Config
                preferences: {}
                users:
                - name: testuser
                  user:
                    token: foobar
                """)),
        };

        [Theory, AutoData]
        public async Task Create_NamespaceSpecifiedForClusterScopedResource_Throws(string kind, string type)
        {
            var apiResource = new V1APIResource { Namespaced = false, Kind = kind };
            var config = await K8sClusterAccessConfig.FromAsync(DummyConfigObject);
            var requestBody = new ResourceRequestBody(type, new()
            {
                ["metadata"] = new JsonObject
                {
                    ["namespace"] = "specified",
                }
            });

            var exception = Invoking(() => K8sResourceReferenceId.Create(apiResource, config, requestBody))
                .Should()
                .Throw<ErrorResponseException>()
                .Which;

            exception.Error.Code.Should().Be("NamespaceNotAllowed");
            exception.Error.Message.Should().Be($"Namespace should be specified for a cluster-scoped resource kind '{kind}'.");
            exception.Error.Target?.ToString().Should().Be("/properties/metadata/namespace");
        }

        [Theory, AutoData]
        public async Task Create_NamespaceNotSpecifiedForNamespacedResource_Throws(string kind, string type)
        {
            var apiResource = new V1APIResource { Namespaced = true, Kind = kind };
            var config = await K8sClusterAccessConfig.FromAsync(DummyConfigObject);
            var requestBody = new ResourceRequestBody(type, []);

            var exception = Invoking(() => K8sResourceReferenceId.Create(apiResource, config, requestBody))
                .Should()
                .Throw<ErrorResponseException>()
                .Which;

            exception.Error.Code.Should().Be("NamespaceNotSpecified");
            exception.Error.Message.Should().Be($"Namespace is not specified for a namespaced resource kind '{kind}'.");
            exception.Error.Target?.ToString().Should().Be("/properties/metadata/namespace");
        }

        [Theory]
        [InlineData("")]
        [InlineData(":")]
        [InlineData("01-group::plural:kind:namespace:name:clusterHostHash")]
        [InlineData("01-group:version::kind:namespace:name:clusterHostHash")]
        [InlineData("01-group:version:plural::namespace:name:clusterHostHash")]
        [InlineData("01-group:version:plural:kind::name:clusterHostHash")]
        [InlineData("01-group:version:plural:kind:namespace::clusterHostHash")]
        [InlineData("01-group:version:plural:kind:namespace:name:")]
        public void Parse_InvalidReferenceId_Throws(string value)
        {
            var exception = Invoking(() => K8sResourceReferenceId.Parse(value)).Should().Throw<ArgumentException>().Subject;
        }

        [Theory]
        [InlineAutoData("")]
        [InlineAutoData("group")]
        public void Parse_ValidReferenceId_ReturnsParsedId(string group, string version, string plural, string kind, string @namespace, string name, string clusterHostHash)
        {
            var expected = new K8sResourceReferenceId(group, version, plural, kind, @namespace, name, clusterHostHash);

            var parsed = K8sResourceReferenceId.Parse(expected.ToString());

            parsed.Should().Be(expected);
        }

        [Theory]
        [InlineAutoData("")]
        [InlineAutoData("group")]
        public void ToString_ValidReferenceId_ReturnsFormattedIdString(string group, string version, string plural, string kind, string @namespace, string name, string clusterHostHash)
        {
            var sut = new K8sResourceReferenceId(group, version, plural, kind, @namespace, name, clusterHostHash);

            sut.ToString().Should().Be($"01-{group}%3a{version}%3a{plural}%3a{kind}%3a{@namespace}%3a{name}%3a{clusterHostHash}");
        }
    }
}
