// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AutoFixture.Xunit2;

namespace Azure.Deployments.Extensibility.TestFixtures.Attributes
{
    public class InlineAutoMoqDataAttribute(params object[] objects)
        : InlineAutoDataAttribute(new AutoMoqDataAttribute(), objects)
    {
    }
}
