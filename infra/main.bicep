@minLength(1)
@description('Primary location for all resources')
param location string

@minLength(1)
@maxLength(64)
@description('Name of the the environment which is used to generate a short unique hash used in all resources.')
param environmentName string = resourceGroup().name

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
  }
}

// Expose useful outputs from platform and app modules
output webAppHostName string = app.outputs.webAppHostName
output botServiceName string = app.outputs.botServiceName
output openAiServiceName string = platform.outputs.openAiServiceName
