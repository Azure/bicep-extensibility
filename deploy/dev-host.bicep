param location string = resourceGroup().location
param webAppName string

resource webAppPlan 'Microsoft.Web/serverfarms@2021-03-01' = {
  name: '${webAppName}-plan'
  location: location
  kind: 'linux'
  sku: {
    name: 'B1'
  }
  properties: {
    reserved: true
  }
}

resource webApp 'Microsoft.Web/sites@2021-03-01' = {
  name: webAppName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: webAppPlan.id
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|6.0'
    }
  }

  resource appSettings 'config' = {
    name: 'appsettings'
    properties: {
      THIRD_PARTY_SUB_ID: subscription().subscriptionId
      THIRD_PARTY_RG_NAME: resourceGroup().name
      THIRD_PARTY_RG_LOCATION: location
    }
  }
}

module roleAssignment 'roleassignment.bicep' = {
  name: 'roleassignment'
  params: {
    principalId: webApp.identity.principalId
    roleDefinition: 'b24988ac-6180-42a0-ab88-20f7382dd24c'
  }
}
