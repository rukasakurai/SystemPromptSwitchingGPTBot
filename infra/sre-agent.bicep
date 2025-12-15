// Azure SRE Agent deployment module
// 
// ⚠️ PREVIEW STATUS: This module uses Microsoft.App/agents@2025-05-01-preview
// The API is subject to breaking changes. Full schema is not yet publicly documented.
// Recommended: Deploy via Azure Portal until API reaches GA.
//
// Purpose: Foundation template for future IaC-based SRE Agent deployments
// When GA: Update to stable API version and complete property definitions
//
// USAGE: Deploy at subscription scope, not resource group scope
// az deployment sub create --location eastus2 --template-file sre-agent.bicep --parameters ...

targetScope = 'subscription'

@description('Name of the resource group for the SRE Agent')
param resourceGroupName string

@description('Name of the SRE Agent')
param agentName string

@description('Azure region for the SRE Agent (limited availability: eastus2, swedencentral, australiaeast)')
@allowed([
  'eastus2'
  'swedencentral'
  'australiaeast'
])
param location string = 'eastus2'

@description('Array of resource group names to monitor. Example: ["rg-app-production", "rg-data-production"]')
param monitoredResourceGroupNames array

@description('Permission level for the agent')
@allowed([
  'Reader'
  'Privileged'
])
param permissionLevel string = 'Reader'

@description('Tags to apply to the SRE Agent resource')
param tags object = {}

// Resource group for the SRE Agent itself
resource sreAgentRg 'Microsoft.Resources/resourceGroups@2021-04-01' existing = {
  name: resourceGroupName
}

// Module to deploy SRE Agent within the resource group
module sreAgentModule 'sre-agent-resource.bicep' = {
  name: 'sreAgent'
  scope: sreAgentRg
  params: {
    agentName: agentName
    location: location
    tags: tags
  }
}

// Role assignments for monitored resource groups
// The SRE Agent's managed identity needs read access to monitored resources

// Reader role - read resource properties and status
module readerRoleAssignments 'sre-agent-rbac.bicep' = [for rgName in monitoredResourceGroupNames: {
  name: 'reader-${rgName}'
  scope: resourceGroup(rgName)
  params: {
    principalId: sreAgentModule.outputs.agentPrincipalId
    roleDefinitionId: 'acdd72a7-3385-48ef-bd42-f606fba81ae7' // Reader
  }
}]

// Monitoring Reader role - read metrics and monitoring data
module monitoringReaderRoleAssignments 'sre-agent-rbac.bicep' = [for rgName in monitoredResourceGroupNames: {
  name: 'monitoring-${rgName}'
  scope: resourceGroup(rgName)
  params: {
    principalId: sreAgentModule.outputs.agentPrincipalId
    roleDefinitionId: '43d0d8ad-25c7-4714-9337-8ba259a9fe05' // Monitoring Reader
  }
}]

// Log Analytics Reader role - query Log Analytics workspace
module logAnalyticsReaderRoleAssignments 'sre-agent-rbac.bicep' = [for rgName in monitoredResourceGroupNames: {
  name: 'logs-${rgName}'
  scope: resourceGroup(rgName)
  params: {
    principalId: sreAgentModule.outputs.agentPrincipalId
    roleDefinitionId: '73c42c96-874c-492b-b04d-ab87d138a893' // Log Analytics Reader
  }
}]

// Optional: Website Contributor for remediation actions (only if permissionLevel == 'Privileged')
module websiteContributorRoleAssignments 'sre-agent-rbac.bicep' = [for rgName in monitoredResourceGroupNames: if (permissionLevel == 'Privileged') {
  name: 'website-${rgName}'
  scope: resourceGroup(rgName)
  params: {
    principalId: sreAgentModule.outputs.agentPrincipalId
    roleDefinitionId: 'de139f84-1756-47ae-9be6-808fbbe84772' // Website Contributor
  }
}]

// Outputs
output agentId string = sreAgentModule.outputs.agentId
output agentName string = sreAgentModule.outputs.agentName
output agentPrincipalId string = sreAgentModule.outputs.agentPrincipalId
output agentLocation string = sreAgentModule.outputs.agentLocation
