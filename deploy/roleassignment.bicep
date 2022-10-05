param roleDefinition string
param principalId string

resource contributorRole 'Microsoft.Authorization/roleDefinitions@2022-04-01' existing = {
  scope: tenant()
  name: roleDefinition
}

resource webAppRoleAssignment 'Microsoft.Authorization/roleAssignments@2020-10-01-preview' = {
  name: guid(contributorRole.id, resourceGroup().id, principalId)
  properties: {
    roleDefinitionId: contributorRole.id
    principalId: principalId
  }
}
