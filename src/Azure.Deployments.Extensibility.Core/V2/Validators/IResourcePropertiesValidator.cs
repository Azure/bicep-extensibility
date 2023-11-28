// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Models;
using System.Text.Json.Nodes;

namespace Azure.Deployments.Extensibility.Core.V2.Validators
{
    public interface IResourcePropertiesValidator : IValidator<JsonObject, IReadOnlyList<ErrorDetail>>
    {
    }
}
