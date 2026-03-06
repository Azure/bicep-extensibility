// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;

namespace Azure.Deployments.Extensibility.Core.Tests.Unit.V2.Json
{
    public static class JsonNodeTestExtensions
    {
        public static JsonNodeAssertions Should(this JsonNode? instance) => new(instance);
    }

    public class JsonNodeAssertions(JsonNode? instance) : ReferenceTypeAssertions<JsonNode?, JsonNodeAssertions>(instance)
    {
        protected override string Identifier => "JsonNode";

        public AndConstraint<JsonNodeAssertions> DeepEqual(JsonNode expected, string because = "", params object[] becauseArgs)
        {
            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .ForCondition(JsonNode.DeepEquals(this.Subject, expected))
                .FailWith("Expected {0} but got {1}. Differences: {2}", expected.ToString(), this.Subject?.ToString(), JsonTestHelpers.GetJsonDiff(this.Subject, expected));

            return new AndConstraint<JsonNodeAssertions>(this);
        }
    }
}
