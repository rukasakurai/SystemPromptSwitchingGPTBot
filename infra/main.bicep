targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the the environment which is used to generate a short unique hash used in all resources.')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string

var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))

// Organize resources in a resource group
resource resourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: 'rg-${environmentName}'
  location: location
}

// Deploy platform resources (reusable across use cases)
module platform 'platform.bicep' = {
  name: 'platform'
  scope: resourceGroup
  params: {
    resourceToken: resourceToken
    location: location
  }
}

// Deploy app resources (Bot and Web App)
module app 'app.bicep' = {
  name: 'app'
  scope: resourceGroup
  params: {
    resourceToken: resourceToken
    location: location
    appServicePlanId: platform.outputs.appServicePlanId
    appInsightsConnectionString: platform.outputs.appInsightsConnectionString
    openAiEndpoint: platform.outputs.openAiEndpoint
    openAiDeploymentName: platform.outputs.openAiDeploymentName
  }
}

// Expose useful outputs from platform and app modules
output webAppHostName string = app.outputs.webAppHostName
output botServiceName string = app.outputs.botServiceName
output openAiServiceName string = platform.outputs.openAiServiceName
