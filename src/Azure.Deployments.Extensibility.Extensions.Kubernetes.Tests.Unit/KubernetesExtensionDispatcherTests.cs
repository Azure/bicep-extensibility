// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.AspNetCore.Exceptions;
using Azure.Deployments.Extensibility.TestFixtures;
using FluentAssertions;
using static FluentAssertions.FluentActions;

namespace Azure.Deployments.Extensibility.Extensions.Kubernetes.Tests.Unit
{
    public class KubernetesExtensionDispatcherTests
    {
        [Theory, AutoMoqData]
        internal void DispatchExtension_InvalidVersion_Throws(string invalidVersion, KubernetesExtensionDispatcher sut)
        {
            Invoking(() => sut.DispatchExtension(invalidVersion))
                .Should()
                .Throw<ErrorResponseException>();
        }

        [Theory]
        [InlineAutoMoqData("1.0.0")]
        [InlineAutoMoqData("1.1.0-alpha")]
        [InlineAutoMoqData("1.1.2-prerelease+meta")]
        [InlineAutoMoqData("1.1.2-prerelease+meta")]
        [InlineAutoMoqData("2.0.0-rc.1+build.123")]
        [InlineAutoMoqData("2.0.0-beta-a.b-c-somethinglong+build.1-aef.1-its-okay")]
        internal void DispatchExtension_ValidVersion_DoesNotThrow(string validVersion, KubernetesExtensionDispatcher sut)
        {
            Invoking(() => sut.DispatchExtension(validVersion))
                .Should()
                .NotThrow();
        }
    }
}
