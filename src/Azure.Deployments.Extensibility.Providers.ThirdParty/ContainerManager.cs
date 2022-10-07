// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Applications.Containers;

namespace Azure.Deployments.Extensibility.Providers.ThirdParty;

public interface IContainerManager
{
    Task Delete(string containerName, CancellationToken cancellation);

    Task<Uri> Create(string containerName, string image, int externalPort, CancellationToken cancellation);
}

internal class ContainerManager : IContainerManager
{
    private const string ManagedEnvironmentName = "bicep-3party-ma";
    private const string ContainerAppPrefix = "bicep-3party-ca-";

    private readonly IAppSettings appSettings;
    private readonly ArmClient armClient;

    public ContainerManager(IAppSettings appSettings, ArmClient armClient)
    {
        this.appSettings = appSettings;
        this.armClient = armClient;
    }

    public async Task Delete(string containerName, CancellationToken cancellation)
    {
        var containerAppName = $"{ContainerAppPrefix}{containerName}";
        var resourceGroup = armClient.GetResourceGroupResource(ResourceGroupResource.CreateResourceIdentifier(appSettings.ThirdPartySubId, appSettings.ThirdPartyRgName));

        if (await resourceGroup.GetContainerApps().ExistsAsync(containerAppName, cancellation))
        {
            var containerApp = await resourceGroup.GetContainerApps().GetAsync(containerAppName, cancellation);

            await containerApp.Value.DeleteAsync(
                WaitUntil.Completed,
                cancellation);
        }
    }

    public async Task<Uri> Create(string containerName, string image, int externalPort, CancellationToken cancellation)
    {
        var containerAppName = $"{ContainerAppPrefix}{containerName}";
        var resourceGroup = armClient.GetResourceGroupResource(ResourceGroupResource.CreateResourceIdentifier(appSettings.ThirdPartySubId, appSettings.ThirdPartyRgName));

        var managedEnvironment = await TryGetManagedEnvironment(resourceGroup, ManagedEnvironmentName, cancellation);
        if (managedEnvironment is null)
        {
            var result = await resourceGroup.GetManagedEnvironments().CreateOrUpdateAsync(
                WaitUntil.Completed,
                ManagedEnvironmentName,
                new(appSettings.ThirdPartyRgLocation),
                cancellation);

            managedEnvironment = result.Value;
        }

        var containerApp = await TryGetContainerApp(resourceGroup, containerAppName, cancellation);
        if (containerApp is null)
        {
            var appData = new ContainerAppData(appSettings.ThirdPartyRgLocation)
            {
                ManagedEnvironmentId = managedEnvironment.Id,
                Configuration = new()
                {
                    Ingress = new()
                    {
                        External = true,
                        TargetPort = externalPort,
                    }
                },
                Template = new()
                {
                }
            };
            appData.Template.Containers.Add(new()
            {
                Image = image,
                Name = containerName,
            });

            var result = await resourceGroup.GetContainerApps().CreateOrUpdateAsync(
                WaitUntil.Completed,
                containerAppName,
                appData,
                cancellation);

            containerApp = result.Value;
        }

        return new Uri($"https://{containerApp.Data.LatestRevisionFqdn}");
    }

    private async Task<ContainerAppResource?> TryGetContainerApp(ResourceGroupResource resourceGroup, string containerAppName, CancellationToken cancellation)
    {
        try
        {
            return await resourceGroup.GetContainerAppAsync(containerAppName, cancellation);
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    private async Task<ManagedEnvironmentResource?> TryGetManagedEnvironment(ResourceGroupResource resourceGroup, string name, CancellationToken cancellation)
    {
        try
        {
            return await resourceGroup.GetManagedEnvironmentAsync(name, cancellation);
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }
}
