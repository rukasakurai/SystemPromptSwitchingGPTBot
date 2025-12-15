// SRE Agent resource definition module
// This module is called by sre-agent.bicep and should not be deployed directly

@description('Name of the SRE Agent')
param agentName string

@description('Azure region for the SRE Agent')
param location string

@description('Tags to apply to the SRE Agent resource')
param tags object = {}

// SRE Agent resource
// Note: properties schema is not fully documented. This is a minimal valid template.
resource sreAgent 'Microsoft.App/agents@2025-05-01-preview' = {
  name: agentName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  tags: tags
  properties: {
    // Properties are not fully documented in preview API
    // Expected fields based on community samples and Azure Portal behavior:
    // - resourceGroups: array of resource group IDs to monitor
    // - permissionLevel: 'Reader' or 'Privileged'
    // Actual schema may differ - verify with Azure Portal JSON export
  }
}

// Outputs
output agentId string = sreAgent.id
output agentName string = sreAgent.name
output agentPrincipalId string = sreAgent.identity.principalId
output agentLocation string = sreAgent.location
