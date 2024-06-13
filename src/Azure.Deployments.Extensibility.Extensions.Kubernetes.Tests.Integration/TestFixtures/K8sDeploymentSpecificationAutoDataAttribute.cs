// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AutoFixture;
using AutoFixture.Xunit2;

namespace Azure.Deployments.Extensibility.Extensions.Kubernetes.Tests.Integration.TestFixtures
{
    public sealed class K8sDeploymentSpecificationAutoDataAttribute(bool generateNamespace = false)
        : AutoDataAttribute(() =>
        {
            var fixture = new Fixture();

            var name = fixture.Create("name");
            var @namespace = generateNamespace ? fixture.Create("namespace") : null;

            fixture.Inject(new K8sDeploymentSpecification(name, @namespace));

            return fixture;
        })
    {
    }
}
