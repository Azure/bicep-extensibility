// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AutoFixture;
using Json.Schema;

namespace Azure.Deployments.Extensibility.Core.Tests.Unit.Fixtures.Customizations
{
    public class JsonSchemaCustomization : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            var sampleSchema = new JsonSchemaBuilder()
                .Properties(
                    ("foo", new JsonSchemaBuilder().Type(SchemaValueType.String).Pattern("bingo")),
                    ("bar", new JsonSchemaBuilder().Type(SchemaValueType.Boolean)))
                .AdditionalProperties(false);

            fixture.Register<JsonSchema>(() => sampleSchema);
        }
    }
}
