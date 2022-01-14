@description('Base name for the deployment')
param baseName string

var location = resourceGroup().location

resource storageAccount 'Microsoft.Storage/storageAccounts@2019-06-01' = {
  name: toLower('${baseName}${uniqueString(resourceGroup().id)}')
  location: location
  sku: {
    name: 'Standard_GRS'
  }
  kind: 'Storage'
}

resource serverFarm 'Microsoft.Web/serverfarms@2020-06-01' = {
  name: baseName
  location: location
  kind: 'functionapp,linux'
  properties: {
    reserved: true
  }
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
}

resource functionApp 'Microsoft.Web/sites@2020-06-01' = {
  name: baseName
  location: location
  kind: 'functionapp,linux'
  properties: {
    httpsOnly: true
    serverFarmId: serverFarm.id
    siteConfig: {
      appSettings: [
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~3'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
        }
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: appInsights.properties.InstrumentationKey
        }
      ]
    }
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02-preview' = {
  name: baseName
  kind: ''
  location: location
  tags: {
    'hidden-link:${resourceId('Microsoft.Web/sites', baseName)}': 'Resource'
  }
  properties: {
    Application_Type: 'web'
  }
}
