// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Reflection;
using System.Runtime.Loader;
using Azure.Deployments.Extensibility.TypeGeneration.Attributes;
using Azure.Deployments.Extensibility.TypeGeneration.Semantics;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Azure.Deployments.Extensibility.TypeGeneration.Tests.Support;

internal static class LegacyPropertyProbe
{
    public static (string Name, string? Description, PropertyFlags Flags, bool IsSecure)[] Read(
        Compilation compilation,
        string typeName)
    {
        using var stream = new MemoryStream();
        var emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
        stream.Position = 0;
        var context = new AssemblyLoadContext("PropertyCompatibility", isCollectible: true);
        try
        {
            var type = context.LoadFromStream(stream).GetType(typeName, throwOnError: true)!;
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(property =>
            {
                var annotation = property.GetCustomAttributes<TypePropertyAttribute>(true).FirstOrDefault();
                return ($"{property.Name[..1].ToLowerInvariant()}{property.Name[1..]}",
                    annotation?.Description,
                    annotation is null ? PropertyFlags.None : (PropertyFlags)annotation.Flags,
                    annotation?.IsSecure ?? false);
            }).ToArray();
        }
        finally
        {
            context.Unload();
        }
    }
}
