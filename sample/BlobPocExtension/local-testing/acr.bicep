// Provisions an Azure Container Registry to host the published BlobPocExtension image.
// Anonymous (unauthenticated) pull is disabled and the admin user is turned off, so all
// pull/push access must go through Azure AD (az acr login / managed identity).
//
// Deploy (PowerShell):
//   az group create --name ligar-test --location eastus
//   az deployment group create `
//     --resource-group ligar-test `
//     --template-file acr.bicep `
//     --parameters registryName=ligartestacr$((Get-Random -Maximum 99999))

targetScope = 'resourceGroup'

@description('Globally-unique registry name: 5-50 alphanumeric characters.')
@minLength(5)
@maxLength(50)
param registryName string

@description('Azure region for the registry. Defaults to the resource group location.')
param location string = resourceGroup().location

@description('Registry SKU. Standard or Premium support the anonymous-pull toggle.')
@allowed([
  'Standard'
  'Premium'
])
param sku string = 'Standard'

resource registry 'Microsoft.ContainerRegistry/registries@2023-11-01-preview' = {
  name: registryName
  location: location
  sku: {
    name: sku
  }
  properties: {
    // Force Azure AD auth for every operation.
    adminUserEnabled: false
    anonymousPullEnabled: false
  }
}

@description('Login server used to tag and push images, e.g. <registry>.azurecr.io.')
output loginServer string = registry.properties.loginServer

@description('Registry resource name.')
output registryName string = registry.name
