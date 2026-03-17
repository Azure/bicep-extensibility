// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Extensibility.Core.V2.Contracts.Models;
using System.Collections.Concurrent;
using System.Text.Json.Nodes;

namespace MagicEightBallExtension.Data;

/// <summary>
/// Thread-safe in-memory store for fortune resources and pending LRO operations.
/// </summary>
public class FortuneStore
{
    private static readonly string[] Fortunes =
    [
        "It is certain.",
        "Without a doubt.",
        "You may rely on it.",
        "Yes, definitely.",
        "It is decidedly so.",
        "As I see it, yes.",
        "Most likely.",
        "Outlook good.",
        "Signs point to yes.",
        "Reply hazy, try again.",
        "Ask again later.",
        "Better not tell you now.",
        "Cannot predict now.",
        "Concentrate and ask again.",
        "Don't count on it.",
        "My reply is no.",
        "My sources say no.",
        "Outlook not so good.",
        "Very doubtful.",
        "The cosmos need more time to decide...",
    ];

    private readonly ConcurrentDictionary<string, Resource> resources = new();

    private readonly ConcurrentDictionary<string, PendingOperation> pendingOperations = new();

    /// <summary>
    /// Generates a random fortune string.
    /// </summary>
#pragma warning disable CA5394 // Random is fine here — fortunes don't need cryptographic security
    public string GetRandomFortune() => Fortunes[Random.Shared.Next(Fortunes.Length)];
#pragma warning restore CA5394

    /// <summary>
    /// Determines if this fortune triggers a long-running operation (cosmic contemplation).
    /// The last fortune in the list triggers LRO for demo purposes.
    /// </summary>
    public static bool RequiresCosmicContemplation(string fortune) =>
        fortune == Fortunes[^1];

    /// <summary>
    /// Builds a resource key from type + name.
    /// </summary>
    public static string GetResourceKey(string type, string name) => $"{type}::{name}";

    public Resource? TryGetResource(string key) =>
        resources.TryGetValue(key, out var resource) ? resource : null;

    public Resource StoreResource(string key, Resource resource)
    {
        resources[key] = resource;
        return resource;
    }

    public Resource? RemoveResource(string key) =>
        resources.TryRemove(key, out var resource) ? resource : null;

    public string CreatePendingOperation(Resource resource, string key)
    {
        var operationId = Guid.NewGuid().ToString("N");
        pendingOperations[operationId] = new PendingOperation(resource, key, DateTimeOffset.UtcNow);
        return operationId;
    }

    public PendingOperation? TryGetPendingOperation(string operationId) =>
        pendingOperations.TryGetValue(operationId, out var op) ? op : null;

    public void CompletePendingOperation(string operationId)
    {
        if (pendingOperations.TryRemove(operationId, out var op))
        {
            resources[op.ResourceKey] = op.Resource;
        }
    }

    public record PendingOperation(Resource Resource, string ResourceKey, DateTimeOffset CreatedAt);
}
