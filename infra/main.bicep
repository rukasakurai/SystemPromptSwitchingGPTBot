@minLength(1)
@description('Primary location for all resources')
param location string = resourceGroup().location

@minLength(1)
@maxLength(64)
@description('Name of the the environment which is used to generate a short unique hash used in all resources.')
param environmentName string = resourceGroup().name

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

var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))

// Deploy platform resources (reusable across use cases)
module platform 'platform.bicep' = {
  name: 'platform'
  params: {
    resourceToken: resourceToken
    location: location
  }
}

// Deploy app resources (Bot and Web App)
module app 'app.bicep' = {
  name: 'app'
  params: {
    resourceToken: resourceToken
    location: location
    appServicePlanId: platform.outputs.appServicePlanId
    appInsightsConnectionString: platform.outputs.appInsightsConnectionString
    openAiEndpoint: platform.outputs.openAiEndpoint
    openAiDeploymentName: platform.outputs.openAiDeploymentName
    microsoftAppType: microsoftAppType
    microsoftAppId: microsoftAppId
    microsoftAppPassword: microsoftAppPassword
    microsoftAppTenantId: microsoftAppTenantId
  }
}

// Expose useful outputs from platform and app modules
output webAppHostName string = app.outputs.webAppHostName
output botServiceName string = app.outputs.botServiceName
output openAiServiceName string = platform.outputs.openAiServiceName
