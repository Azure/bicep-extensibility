// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using Azure.Deployments.Extensibility.TypeGeneration.Semantics;

namespace Azure.Deployments.Extensibility.TypeGeneration.Tests.Support;

internal static class SemanticModelComparer
{
    public static bool AreEqual(object? expected, object? actual)
        => AreEqual(expected, actual, new HashSet<(object Left, object Right)>(NodePairComparer.Instance));

    private static bool AreEqual(
        object? expected,
        object? actual,
        HashSet<(object Left, object Right)> visited)
    {
        if (ReferenceEquals(expected, actual))
        {
            return true;
        }

        if (expected is null || actual is null || expected.GetType() != actual.GetType())
        {
            return false;
        }

        var type = expected.GetType();
        if (type.IsEnum || expected is string or bool or int or long)
        {
            return expected.Equals(actual);
        }

        if (!type.IsValueType && !visited.Add((expected, actual)))
        {
            return true;
        }

        if (expected is ImmutableDictionary<string, ObjectTypeDefinition> expectedCases &&
            actual is ImmutableDictionary<string, ObjectTypeDefinition> actualCases)
        {
            return expectedCases.Keys.Order(StringComparer.Ordinal)
                .SequenceEqual(actualCases.Keys.Order(StringComparer.Ordinal), StringComparer.Ordinal) &&
                expectedCases.All(pair => AreEqual(pair.Value, actualCases[pair.Key], visited));
        }

        if (expected is IEnumerable expectedSequence && actual is IEnumerable actualSequence)
        {
            var expectedItems = expectedSequence.Cast<object?>().ToArray();
            var actualItems = actualSequence.Cast<object?>().ToArray();
            return expectedItems.Length == actualItems.Length &&
                expectedItems.Zip(actualItems).All(pair => AreEqual(pair.First, pair.Second, visited));
        }

        if (type.Namespace != typeof(TypeDefinition).Namespace)
        {
            throw new InvalidOperationException($"Semantic comparison does not support '{type.FullName}'.");
        }

        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.PropertyType != typeof(SourceLocation))
            .All(property => AreEqual(property.GetValue(expected), property.GetValue(actual), visited));
    }

    private sealed class NodePairComparer : IEqualityComparer<(object Left, object Right)>
    {
        public static NodePairComparer Instance { get; } = new();

        public bool Equals((object Left, object Right) x, (object Left, object Right) y)
            => ReferenceEquals(x.Left, y.Left) && ReferenceEquals(x.Right, y.Right);

        public int GetHashCode((object Left, object Right) pair)
            => HashCode.Combine(RuntimeHelpers.GetHashCode(pair.Left), RuntimeHelpers.GetHashCode(pair.Right));
    }
}
