param resourceToken string
param location string

resource openAiService 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: 'oai-${resourceToken}'
  location: location
  kind: 'OpenAI'
  sku: {
    name: 'S0'
  }
}

resource openAIModel 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = {
  parent: openAiService
  name: 'gpt-35-turbo'
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-35-turbo'
      version: '0613'
    }
  }
  sku: {
    name: 'Standard'
    capacity: 20
  }
}

resource appServicePlan 'Microsoft.Web/serverfarms@2022-03-01' = {
  name: 'plan-${resourceToken}'
  location: location
  sku: {
    name: 'P1'
    capacity: 1
  }
}

resource webApp 'Microsoft.Web/sites@2022-03-01' = {
  name: 'web-${resourceToken}'
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
  }
}

// Azure Bot Service (WIP)
resource botService 'Microsoft.BotService/botServices@2021-03-01' = {
  name: 'bot-${resourceToken}'
  location: 'global'
  kind: 'azurebot'
  sku: {
    name: 'S1' // Pricing tier
  }
  properties: {
    displayName: 'bot-${resourceToken}'
    endpoint: 'https://${webApp.properties.defaultHostName}/api/messages'
    msaAppId: guid(subscription().id, resourceGroup().id, 'bot', resourceToken)
    msaAppType: 'MultiTenant' // Should maybe make this Single Tenant later
  }
}

// Log Analytics workspace (WIP)
resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2020-08-01' = {
  name: 'log-${resourceToken}'
  location: location
}

// Application Insights (WIP)
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'ai-${resourceToken}'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspace.id
  }
}

// Add Application Insights connection string to App Settings (WIP)
resource appSettings 'Microsoft.Web/sites/config@2022-03-01' = {
  parent: webApp
  name: 'appsettings'
  properties: {
    APPINSIGHTS_INSTRUMENTATIONKEY: appInsights.properties.InstrumentationKey
  }
}

// Role Assignment for Managed Identity to access Azure OpenAI (WIP)
module openAiRoleUser 'role.bicep' = {
  name: 'openai-role-user'
  params: {
    principalId: webApp.identity.principalId
    roleDefinitionId: '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd'
    principalType: 'ServicePrincipal'
  }
}
