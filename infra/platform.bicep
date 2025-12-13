param resourceToken string
param location string

@description('The version of the GPT-4o model to deploy')
param gptModelVersion string = '2024-08-06'

// Azure OpenAI Service
resource openAiService 'Microsoft.CognitiveServices/accounts@2025-09-01' = {
  name: 'oai-${resourceToken}'
  location: location
  kind: 'OpenAI'
  sku: {
    name: 'S0'
  }
  properties: {
    // Some tenants enforce keyless auth via Azure Policy.
    // Setting this explicitly avoids drift and matches the intended Entra ID / managed identity flow.
    disableLocalAuth: true
    // Required by the 2025-09-01 API version.
    publicNetworkAccess: 'Enabled'
  }
}

var openAiEndpoint = 'https://${openAiService.name}.openai.azure.com/'

// Azure OpenAI Model Deployment
resource openAIModel 'Microsoft.CognitiveServices/accounts/deployments@2025-09-01' = {
  parent: openAiService
  name: 'gpt-4o'
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-4o'
      version: gptModelVersion
    }
  }
  sku: {
    name: 'GlobalStandard'
    capacity: 20
  }
}

// Log Analytics workspace
resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2025-07-01' = {
  name: 'log-${resourceToken}'
  location: location
}

// Application Insights
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'appi-${resourceToken}'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspace.id
  }
}

// Azure App Service Plan
resource appServicePlan 'Microsoft.Web/serverfarms@2025-03-01' = {
  name: 'plan-${resourceToken}'
  location: location
  sku: {
    name: 'P0v3'
    capacity: 1
  }
}

// Outputs for use by other modules
output openAiServiceId string = openAiService.id
output openAiServiceName string = openAiService.name
output openAiEndpoint string = openAiEndpoint
output openAiDeploymentName string = openAIModel.name
output appInsightsInstrumentationKey string = appInsights.properties.InstrumentationKey
output appInsightsConnectionString string = appInsights.properties.ConnectionString
output appServicePlanId string = appServicePlan.id
