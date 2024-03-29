// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Microsoft.Extensions.Configuration;

namespace Azure.Deployments.Extensibility.Providers.ThirdParty;

public interface IAppSettings
{
    public string ThirdPartySubId { get; }

    public string ThirdPartyRgName { get; }

    public string ThirdPartyRgLocation { get; }
}

internal class AppSettings : IAppSettings
{
    private readonly IConfiguration configuration;

    public AppSettings(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    public string ThirdPartySubId => configuration.GetValue<string>("THIRD_PARTY_SUB_ID") ?? throw new InvalidOperationException("Configuration THIRD_PARTY_SUB_ID not found.");

    public string ThirdPartyRgName => configuration.GetValue<string>("THIRD_PARTY_RG_NAME") ?? throw new InvalidOperationException("Configuration THIRD_PARTY_RG_NAME not found.");

    public string ThirdPartyRgLocation => configuration.GetValue<string>("THIRD_PARTY_RG_LOCATION") ?? throw new InvalidOperationException("Configuration THIRD_PARTY_RG_LOCATION not found.");
}
