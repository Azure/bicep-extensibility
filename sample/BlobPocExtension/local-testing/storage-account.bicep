// Provisions a Storage account for locally testing the BlobPocExtension and grants the
// signed-in user the data-plane role required to create/list/delete blob containers.
//
// Deploy (PowerShell):
//   az group create --name bicep-poc-rg --location eastus
//   $me = az ad signed-in-user show --query id -o tsv
//   az deployment group create `
//     --resource-group bicep-poc-rg `
//     --template-file storage-account.bicep `
//     --parameters storageAccountName=mystgacct$((Get-Random -Maximum 99999)) principalId=$me

targetScope = 'resourceGroup'

@description('Globally-unique Storage account name: 3-24 lowercase letters and numbers. Matches the extension\'s ^[a-z0-9]{3,24}$ rule.')
@minLength(3)
@maxLength(24)
param storageAccountName string

@description('Object (principal) ID to grant data-plane access. Typically your own user: az ad signed-in-user show --query id -o tsv')
param principalId string

@description('Azure region for the account. Defaults to the resource group location.')
param location string = resourceGroup().location

@description('Principal type for the role assignment.')
@allowed([
  'User'
  'Group'
  'ServicePrincipal'
])
param principalType string = 'User'

// Built-in role: Storage Blob Data Contributor — read/write/delete blob data-plane access.
var storageBlobDataContributorRoleId = 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    supportsHttpsTrafficOnly: true
    // The extension authenticates with Azure AD (DefaultAzureCredential), not keys.
    allowSharedKeyAccess: false
  }
}

resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccount.id, principalId, storageBlobDataContributorRoleId)
  scope: storageAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageBlobDataContributorRoleId)
    principalId: principalId
    principalType: principalType
  }
}

@description('Use this value as the accountName in the extension requests.')
output storageAccountName string = storageAccount.name

@description('Blob service endpoint of the provisioned account.')
output blobEndpoint string = storageAccount.properties.primaryEndpoints.blob
