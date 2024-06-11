// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Xunit2;
using Azure.Deployments.Extensibility.AspNetCore.Exceptions;
using Azure.Deployments.Extensibility.Extensions.Kubernetes.Api;
using Azure.Deployments.Extensibility.Extensions.Kubernetes.Models;
using FluentAssertions;
using System.Text.Json.Nodes;
using static FluentAssertions.FluentActions;

namespace Azure.Deployments.Extensibility.Extensions.Kubernetes.Tests.Unit.Api
{
    public class K8sClusterScopedApiTests
    {
        [Theory, NamespacedK8sObjectAutoData]
        internal async Task PatchObjectAsync_NamespacedObject_Throws(K8sObject namespacedObject, K8sClusterScopedApi sut)
        {
            await Invoking(() => sut.PatchObjectAsync(namespacedObject, default, default))
                .Should()
                .ThrowAsync<ErrorResponseException>();
        }

        [Theory, NamespacedK8sObjectAutoData]
        internal async Task GetObjectAsync_NamespacedObject_Throws(K8sObject namespacedObject, string serverHost, K8sClusterScopedApi sut)
        {
            var namespacedIdentifiers = K8sObjectIdentifiers.Create(namespacedObject, serverHost);

            await Invoking(() => sut.GetObjectAsync(namespacedIdentifiers, default))
                .Should()
                .ThrowAsync<ErrorResponseException>();
        }

        [Theory, NamespacedK8sObjectAutoData]
        internal async Task DeleteObjectAsync_NamespacedObject_Throws(K8sObject namespacedObject, string serverHost, K8sClusterScopedApi sut)
        {
            var namespacedIdentifiers = K8sObjectIdentifiers.Create(namespacedObject, serverHost);

            await Invoking(() => sut.DeleteObjectAsync(namespacedIdentifiers, default))
                .Should()
                .ThrowAsync<ErrorResponseException>();
        }

        public class NamespacedK8sObjectAutoDataAttribute : AutoDataAttribute
        {
            public NamespacedK8sObjectAutoDataAttribute()
                : base(CreateFixture)
            {
            }

            private static IFixture CreateFixture()
            {
                var fixture = new Fixture().Customize(new AutoMoqCustomization());

                fixture.Inject(new K8sObject(fixture.Create<GroupVersionKind>(), new JsonObject
                {
                    ["metadata"] = new JsonObject
                    {
                        ["name"] = fixture.Create<string>(),
                        ["namespace"] = fixture.Create<string>(),
                    }
                }));

                return fixture;
            }
        }
    }
}
