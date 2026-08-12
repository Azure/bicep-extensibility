// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace MagicEightBallExtension.Models;

/// <summary>
/// The base properties of a Fortune resource.
/// </summary>
public record FortuneProperties
{
    public required string Name { get; init; }

    public string? Question { get; init; }

    public string? Fortune { get; init; }

    public string? AnsweredAt { get; init; }
}

/// <summary>
/// The properties of the hosted Fortune resource.
/// Adds confidence and mood on top of the base properties.
/// </summary>
public record FortunePropertiesV2 : FortuneProperties
{
    public int? Confidence { get; init; }

    public string? Mood { get; init; }
}

/// <summary>
/// The identifiers that uniquely identify a Fortune resource.
/// </summary>
public record FortuneIdentifiers
{
    public required string Name { get; init; }
}
