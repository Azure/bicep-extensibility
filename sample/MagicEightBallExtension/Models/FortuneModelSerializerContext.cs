// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using MagicEightBallExtension.Models;
using System.Text.Json.Serialization;

namespace MagicEightBallExtension;

/// <summary>
/// Custom source-generated JSON serializer context for extension-specific types.
/// Added to the serializer options chain so the typed handler base classes can
/// serialize/deserialize the Fortune model types.
/// </summary>
[JsonSerializable(typeof(FortuneProperties))]
[JsonSerializable(typeof(FortunePropertiesV2))]
[JsonSerializable(typeof(FortuneIdentifiers))]
internal partial class FortuneModelSerializerContext : JsonSerializerContext
{
}
