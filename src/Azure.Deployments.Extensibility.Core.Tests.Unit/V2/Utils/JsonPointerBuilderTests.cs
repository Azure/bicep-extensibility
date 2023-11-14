// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Utils;
using FluentAssertions;
using Json.Pointer;
using Xunit;

namespace Azure.Deployments.Extensibility.Core.Tests.Unit.V2.Utils
{
    public class JsonPointerBuilderTests
    {
        private record Foobar(Foo Foo);

        private record Foo(Bar Bar);

        private record Bar(string Value);

        [Fact]
        public void Build_PropertyExpression_ReturnsJsonPointerInCamelCase()
        {
            var jsonPointer = JsonPointerBuilder.Build<Foobar>(x => x.Foo.Bar.Value);

            jsonPointer.ToString().Should().Be("/foo/bar/value");
        }
    }
}
