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
using Azure.Deployments.Extensibility.DevHost.AzureContext;
using static Microsoft.Azure.Management.Fluent.Azure;
using Microsoft.Azure.Management.AppService.Fluent.Models;
using JetBrains.Annotations;
using k8s.Models;

namespace Azure.Deployments.Extensibility.DevHost.ACI
{
    public interface IAzureContainerInstanceHost
    {
        Task<string> CreateContainerGroupAsync(
            string resourceGroupName,
            string containerGroupPrefix,
            string image,
            int externalPort,
            CancellationToken cancellation);
    }

    public class AzureContainerInstanceHost : IAzureContainerInstanceHost
    {
        private IAzure AzureContext { get; }

        public AzureContainerInstanceHost([NotNull]IAzureRequestContext requestContext)
        {
            this.AzureContext = requestContext.GetAzureRequestContext();
        }

        public async Task<string> CreateContainerGroupAsync(
            string resourceGroupName,
            string containerGroupPrefix,
            string image,
            int externalPort,
            CancellationToken cancellation)
        {
            // Get the resource group's region
            IResourceGroup resGroup = AzureContext.ResourceGroups.GetByName(name: resourceGroupName);
            Region azureRegion = resGroup.Region;

            var containerGroupName = $"{containerGroupPrefix}-aci";

            await this.AzureContext.ContainerGroups.Define(containerGroupName)
                    .WithRegion(azureRegion)
                    .WithExistingResourceGroup(resourceGroupName)
                    .WithLinux()
                    .WithPublicImageRegistryOnly()
                    .WithoutVolume()
                    .DefineContainerInstance(containerGroupPrefix + "-1")
                        .WithImage(image)
                        .WithExternalTcpPort(externalPort)
                        .WithCpuCoreCount(1.0)
                        .WithMemorySizeInGB(1)
                        .Attach()
                    .WithDnsPrefix(containerGroupPrefix)
                    .CreateAsync(cancellation);

            // Poll for the container group
            IContainerGroup? containerGroup = this.AzureContext.ContainerGroups.GetByResourceGroup(resourceGroupName, containerGroupName);

            while (containerGroup == null)
            {
                await Task.Delay(millisecondsDelay: 1000);

                containerGroup = this.AzureContext.ContainerGroups.GetByResourceGroup(resourceGroupName, containerGroupName);
            }

            // Poll until the container group is running
            while (containerGroup.State != "Running")
            {
                await Task.Delay(millisecondsDelay: 1000);
            }

            return containerGroup.Fqdn;
        }
    }
}
