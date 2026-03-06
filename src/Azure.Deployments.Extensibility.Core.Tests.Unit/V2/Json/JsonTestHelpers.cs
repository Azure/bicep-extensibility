// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Nodes;
using JsonDiffPatchDotNet;

namespace Azure.Deployments.Extensibility.Core.Tests.Unit.V2.Json
{
    public static class JsonTestHelpers
    {
        public static string? GetJsonDiff(JsonNode? first, JsonNode? second)
        {
            const int truncate = 100;
            var diff = new JsonDiffPatch(new Options { TextDiff = TextDiffMode.Simple })
                .Diff(first?.ToJsonString(), second?.ToJsonString());

            if (diff is null)
            {
                return null;
            }

            // JsonDiffPatch.Diff returns null if there are no diffs
            var lineLogs = diff.Split('\n').Take(truncate).ToList();

            if (lineLogs.Count >= truncate)
            {
                lineLogs.Add("... truncated ...");
            }

            return string.Join("\n", lineLogs);
        }
    }
}
