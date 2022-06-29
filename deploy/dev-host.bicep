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
  properties: {
    serverFarmId: webAppPlan.id
    siteConfig: {
      linuxFxVersion: 'DOTNET|6.0'
    }
  }
}
