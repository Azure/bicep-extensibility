// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Azure.Deployments.Extensibility.Providers.Kubernetes.Extensions
{
    public static class StringExtensions
    {
        public static bool IsBase64Encoded(this string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            var buffer = new Span<byte>(new byte[value.Length]);

            return Convert.TryFromBase64String(value, buffer, out _);
        }
    }
}
