param resourceToken string
param location string
param appServicePlanId string
param appInsightsConnectionString string
param openAiEndpoint string
param openAiDeploymentName string

@description('Bot Framework app type. Use SingleTenant to avoid deprecated MultiTenant bots.')
@allowed([
  'SingleTenant'
  'MultiTenant'
  'UserAssignedMSI'
])
param microsoftAppType string = 'SingleTenant'

@description('Microsoft App ID for the bot: the Entra app registration Application (client) ID that represents the bot identity. This must match the Bot Service "Microsoft App ID".')
param microsoftAppId string

@secure()
@description('Microsoft App password (client secret) for the Bot/Entra app registration.')
param microsoftAppPassword string

@description('Microsoft Entra tenant ID for the Bot app registration.')
param microsoftAppTenantId string = subscription().tenantId

// Azure Web App
resource webApp 'Microsoft.Web/sites@2025-03-01' = {
  name: 'web-${resourceToken}'
  location: location
  tags: { 'azd-service-name': 'backend' }
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlanId
    siteConfig: {
      windowsFxVersion: 'DOTNETCORE|8.0'
    }
  }
}

resource openAiService 'Microsoft.CognitiveServices/accounts@2025-09-01' existing = {
  name: 'oai-${resourceToken}'
}

// Azure Bot Service
// NOTE: The msaAppId is generated using a deterministic GUID, but this does NOT create
// the corresponding Microsoft App Registration in Azure AD. You must manually create
// an app registration with this ID in Azure AD before the bot can authenticate.
// Alternatively, consider parameterizing msaAppId and msaAppTenantId to use an existing app registration.
resource botService 'Microsoft.BotService/botServices@2022-09-15' = {
  name: 'bot-${resourceToken}'
  location: 'global'
  kind: 'azurebot'
  sku: {
    name: 'S1' // Pricing tier
  }
  properties: {
    displayName: 'bot-${resourceToken}'
    endpoint: 'https://${webApp.properties.defaultHostName}/api/messages'
    msaAppId: microsoftAppId
    msaAppType: microsoftAppType
    msaAppTenantId: microsoftAppTenantId
  }
}

// Add Application Insights and Azure OpenAI configuration to App Settings
// WARNING: This block overwrites all existing app settings on the web app.
// Be sure to include ALL required app settings here, as any not listed will be removed.
resource appSettings 'Microsoft.Web/sites/config@2025-03-01' = {
  parent: webApp
  name: 'appsettings'
  properties: {
    APPLICATIONINSIGHTS_CONNECTION_STRING: appInsightsConnectionString
    ApplicationInsightsAgent_EXTENSION_VERSION: '~3'
    XDT_MicrosoftApplicationInsights_Mode: 'recommended'
    MicrosoftAppType: microsoftAppType
    MicrosoftAppId: microsoftAppId
    MicrosoftAppPassword: microsoftAppPassword
    MicrosoftAppTenantId: microsoftAppTenantId
    OpenAIEndpoint: openAiEndpoint
    OpenAIDeployment: openAiDeploymentName
    // Add any other required app settings below
  }
}

// Role Assignment for Managed Identity to access Azure OpenAI
resource openAiRoleUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(openAiService.id, webApp.id, '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd')
  scope: openAiService
  properties: {
    principalId: webApp.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd')
  }
}

// Outputs
output webAppName string = webApp.name
output webAppHostName string = webApp.properties.defaultHostName
output webAppPrincipalId string = webApp.identity.principalId
output botServiceName string = botService.name
output botMicrosoftAppId string = microsoftAppId
