// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net;

namespace Azure.Deployments.Extensibility.Hosting.Managed.Server;

internal sealed class ExtensionListenAddressSetup : IConfigureOptions<KestrelServerOptions>
{
    internal const string PortVariable = "EXTENSION_PORT";
    internal const string HostVariable = "EXTENSION_HOST";

    private readonly IConfiguration configuration;

    public ExtensionListenAddressSetup(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    public void Configure(KestrelServerOptions options)
    {
        var port = this.configuration[PortVariable];
        var host = this.configuration[HostVariable];

        if (port is null)
        {
            if (host is not null)
            {
                throw new InvalidOperationException(
                    $"{HostVariable} is set to '{host}', but {PortVariable} is not set.");
            }

            return;
        }

        // A code-configured endpoint overrides ASPNETCORE_URLS and other server addresses unless PreferHostingUrls is set.
        options.Listen(ParseHost(host), ParsePort(port));
    }

    private static int ParsePort(string value) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var port) && port is >= 1 and <= IPEndPoint.MaxPort
            ? port
            : throw new InvalidOperationException(
                $"{PortVariable} must be an integer from 1 to {IPEndPoint.MaxPort}, but was '{value}'.");

    private static IPAddress ParseHost(string? value)
    {
        if (value is null)
        {
            return IPAddress.Loopback;
        }

        return IPAddress.TryParse(value, out var address)
            ? address
            : throw new InvalidOperationException($"{HostVariable} must be an IP address, but was '{value}'.");
    }
}
