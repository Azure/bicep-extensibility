// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Extensions;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Azure.Deployments.Extensibility.Core.V2.Constants
{
    public static class JsonDefaults
    {
        public static readonly JsonSerializerOptions SerializerOptions = new ServiceCollection()
            .AddDefaultJsonSerializerOptions()
            .BuildServiceProvider()
            .GetRequiredService<IOptions<JsonOptions>>()
            .Value
            .SerializerOptions;
    }
}
