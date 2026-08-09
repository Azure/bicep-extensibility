// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace BlobPocExtension.Storage;

/// <summary>
/// Readiness check that does a cheap <c>GetProperties</c> against a configured probe account,
/// turning the always-on <c>/health</c> endpoint into a meaningful storage-reachability signal.
/// </summary>
public sealed class StorageHealthCheck : IHealthCheck
{
    private readonly IStorageClientFactory factory;
    private readonly string? probeAccount;

    public StorageHealthCheck(IStorageClientFactory factory, IOptions<BlobPocOptions> options)
    {
        this.factory = factory;
        this.probeAccount = options.Value.ProbeAccountName;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(this.probeAccount))
        {
            return HealthCheckResult.Healthy("No storage probe account configured; skipping live check.");
        }

        try
        {
            var service = this.factory.GetServiceClient(this.probeAccount);
            await service.GetPropertiesAsync(cancellationToken);
            return HealthCheckResult.Healthy($"Storage account '{this.probeAccount}' reachable.");
        }
        catch (RequestFailedException exception)
        {
            return HealthCheckResult.Unhealthy($"Storage account '{this.probeAccount}' is unreachable.", exception);
        }
    }
}
