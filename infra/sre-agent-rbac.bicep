// Role assignment module for SRE Agent
// This module is called by sre-agent.bicep and should not be deployed directly

@description('Principal ID of the SRE Agent managed identity')
param principalId string

@description('Role definition ID (GUID) to assign')
param roleDefinitionId string

// Role assignment at resource group scope
resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, principalId, roleDefinitionId)
  properties: {
    principalId: principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roleDefinitionId)
  }
}

output roleAssignmentId string = roleAssignment.id
