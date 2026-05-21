// SRE Agent resource definition module
// This module is called by sre-agent.bicep and should not be deployed directly

@description('Name of the SRE Agent')
param agentName string

@description('Azure region for the SRE Agent')
param location string

@description('Tags to apply to the SRE Agent resource')
param tags object = {}

// SRE Agent resource. Extend `properties` per your scenario; see
// https://learn.microsoft.com/en-us/azure/templates/microsoft.app/agents
resource sreAgent 'Microsoft.App/agents@2026-01-01' = {
  name: agentName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  tags: tags
  properties: {}
}

// Outputs
output agentId string = sreAgent.id
output agentName string = sreAgent.name
output agentPrincipalId string = sreAgent.identity.principalId
output agentLocation string = sreAgent.location
