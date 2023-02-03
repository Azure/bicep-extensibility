// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Providers.Kubernetes.Extensions;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.Tests.Unit.Extensions
{
    public class StringExtensionsTests
    {
        [Theory]
        [InlineData("Zm9vYmFy")]
        [InlineData("YmljZXA=")]
        [InlineData("VGhlIHF1aWNrIGJyb3duIGZveCBqdW1wcyBvdmVyIHRoZSBsYXp5IGRvZw==")]
        public void IsBase64Encoded_Base64EncodedString_ReturnsTrue(string value)
        {
            var result = value.IsBase64Encoded();

            result.Should().BeTrue();
        }

        [Theory]
        [InlineData("foobar")]
        [InlineData("bicep")]
        [InlineData("The quick brown fox jumps over the lazy dog")]
        public void IsBase64Encoded_NotBase64EncodedString_ReturnsTrue(string value)
        {
            var result = value.IsBase64Encoded();

            result.Should().BeFalse();
        }
    }
}
