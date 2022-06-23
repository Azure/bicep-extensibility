// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Extensibility.Core.Data
{

    public class ExtensibilityErrorContainer
    {
        public ExtensibilityError[]? Errors { get; set; }

        public int? ShouldRetryAfter { get; set; }
    }
}
