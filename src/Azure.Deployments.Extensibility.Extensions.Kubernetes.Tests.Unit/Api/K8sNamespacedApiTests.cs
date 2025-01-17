// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Xunit2;
using Azure.Deployments.Extensibility.AspNetCore.Exceptions;
using Azure.Deployments.Extensibility.Extensions.Kubernetes.Api;
using Azure.Deployments.Extensibility.Extensions.Kubernetes.Client;
using Azure.Deployments.Extensibility.Extensions.Kubernetes.Models;
using FluentAssertions;
using Moq;
using static FluentAssertions.FluentActions;

namespace Azure.Deployments.Extensibility.Extensions.Kubernetes.Tests.Unit.Api
{
    public class K8sNamespacedApiTests
    {
        [Theory, K8sObjectWithoutNamespaceAutoData]
        internal async Task PatchObjectAsync_NamespacedNotSpecified_Throws(K8sObject objectWithoutNamespace, K8sNamespacedApi sut)
        {
            await Invoking(() => sut.PatchObjectAsync(objectWithoutNamespace, default, default))
                .Should()
                .ThrowAsync<ErrorResponseException>();
        }

        [Theory, K8sObjectWithoutNamespaceAutoData]
        internal async Task GetObjectAsync_NamespacedNotSpecified_Throws(K8sObject objectWithoutNamespace, K8sNamespacedApi sut)
        {
            var identifiersWithoutNamespace = K8sObjectIdentifiers.Create(objectWithoutNamespace);

            await Invoking(() => sut.GetObjectAsync(identifiersWithoutNamespace, default))
                .Should()
                .ThrowAsync<ErrorResponseException>();
        }

        [Theory, K8sObjectWithoutNamespaceAutoData]
        internal async Task DeleteObjectAsync_NamespacedNotSpecified_Throws(K8sObject objectWithoutNamespace, K8sNamespacedApi sut)
        {
            var identifiersWithoutNamespace = K8sObjectIdentifiers.Create(objectWithoutNamespace);

            await Invoking(() => sut.DeleteObjectAsync(identifiersWithoutNamespace, default))
                .Should()
                .ThrowAsync<ErrorResponseException>();
        }

        public class K8sObjectWithoutNamespaceAutoDataAttribute : AutoDataAttribute
        {
            public K8sObjectWithoutNamespaceAutoDataAttribute()
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
                    }
                }));

                fixture.Freeze<Mock<IK8sClient>>()
                    .Setup(x => x.DefaultNamespace).Returns((string?)null);

                return fixture;
            }
        }
    }
}
