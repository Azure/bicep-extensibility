// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Core.V2.Models.Validation
{
    public interface IResourcePropertiesValidator : IValidator<JsonObject, IReadOnlyList<ErrorDetail>>
    {
    }
}
