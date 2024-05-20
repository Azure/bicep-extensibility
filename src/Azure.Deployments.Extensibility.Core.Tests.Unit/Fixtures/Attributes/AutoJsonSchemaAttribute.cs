// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AutoFixture;
using AutoFixture.Xunit2;
using Azure.Deployments.Extensibility.Core.Tests.Unit.Fixtures.Customizations;

namespace Azure.Deployments.Extensibility.Core.Tests.Unit.Fixtures.Attributes
{
    public class AutoJsonSchemaAttribute : AutoDataAttribute
    {
        public AutoJsonSchemaAttribute()
            : base(() => new Fixture().Customize(new JsonSchemaCustomization()))
        {
        }
    }
}
