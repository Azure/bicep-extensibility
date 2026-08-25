// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace MagicEightBallExtension.Models;

/// <summary>
/// The properties of a Fortune resource (v1).
/// </summary>
public record FortuneProperties
{
    public required string Name { get; init; }

    public string? Question { get; init; }

    public string? Fortune { get; init; }

    public string? AnsweredAt { get; init; }
}

/// <summary>
/// The identifiers that uniquely identify a Fortune resource.
/// </summary>
public record FortuneIdentifiers
{
    public required string Name { get; init; }
}
