// SRE Agent resource definition module
// This module is called by sre-agent.bicep and should not be deployed directly

@description('Name of the SRE Agent')
param agentName string

@description('Azure region for the SRE Agent')
param location string

@description('Tags to apply to the SRE Agent resource')
param tags object = {}

// SRE Agent resource (Microsoft.App/agents, stable API as of GA in March 2026).
// The `properties` block here is intentionally minimal. Extend it per your scenario;
// see https://learn.microsoft.com/en-us/azure/templates/microsoft.app/agents for the
// current schema (e.g. monitored resource scope, permission level, connectors).
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
