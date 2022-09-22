// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.ContainerInstance.Fluent;
using Microsoft.Azure.Management.ContainerInstance.Fluent.Models;
using Azure.Deployments.Extensibility.Providers.ThirdParty.AzureContext;

namespace Azure.Deployments.Extensibility.Providers.ThirdParty.ACI
{
    public interface IAzureContainerInstanceHost
    {
        Task DeleteContainerGroupAsync(
            string resourceGroupName,
            string containerGroupName,
            CancellationToken cancellation);

        Task<string> CreateContainerGroupAsync(
            string resourceGroupName,
            string containerGroupName,
            string image,
            int externalPort,
            CancellationToken cancellation);
    }

    internal class AzureContainerInstanceHost : IAzureContainerInstanceHost
    {
        private IAzure AzureContext { get; }

        public AzureContainerInstanceHost(IAzureRequestContext requestContext)
        {
            this.AzureContext = requestContext.GetAzureRequestContext();
        }

        public async Task DeleteContainerGroupAsync(
            string resourceGroupName,
            string containerGroupName,
            CancellationToken cancellation)
        {
            await this.AzureContext.ContainerGroups.DeleteByResourceGroupAsync(
                resourceGroupName: resourceGroupName,
                name: containerGroupName,
                cancellationToken: cancellation);
        }

        public async Task<string> CreateContainerGroupAsync(
            string resourceGroupName,
            string containerGroupName,
            string image,
            int externalPort,
            CancellationToken cancellation)
        {
            // Get the resource group's region
            IResourceGroup resGroup = AzureContext.ResourceGroups.GetByName(name: resourceGroupName);
            Region azureRegion = resGroup.Region;

            await this.AzureContext.ContainerGroups.Define(containerGroupName)
                    .WithRegion(azureRegion)
                    .WithExistingResourceGroup(resourceGroupName)
                    .WithLinux()
                    .WithPublicImageRegistryOnly()
                    .WithoutVolume()
                    .DefineContainerInstance(containerGroupName + "-1")
                        .WithImage(image)
                        .WithExternalTcpPort(externalPort)
                        .WithCpuCoreCount(1.0)
                        .WithMemorySizeInGB(1)
                        .Attach()
                    .WithDnsPrefix(containerGroupName)
                    .CreateAsync(cancellation);

            // Poll for the container group
            IContainerGroup? containerGroup = this.AzureContext.ContainerGroups.GetByResourceGroup(resourceGroupName, containerGroupName);

            while (containerGroup == null)
            {
                await Task.Delay(millisecondsDelay: 1000, cancellationToken: cancellation);

                containerGroup = this.AzureContext.ContainerGroups.GetByResourceGroup(resourceGroupName, containerGroupName);
            }

            // Poll until the container group is running
            while (containerGroup.State != "Running")
            {
                await Task.Delay(millisecondsDelay: 1000, cancellationToken: cancellation);
            }

            return containerGroup.Fqdn;
        }
    }
}
