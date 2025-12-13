# Infrastructure Documentation

## Overview

The infrastructure for this project is defined using Azure Bicep templates. The infrastructure is organized into two main categories to support different lifecycle management strategies:

1. **Platform Resources** - Foundational, reusable infrastructure
2. **Use Case Specific Resources** - Application-specific components

## Architecture

```
main.bicep (Subscription-level orchestration)
├── Resource Group
├── platform.bicep (Platform resources)
│   ├── Azure OpenAI Service
│   ├── Azure OpenAI Model Deployment (GPT-3.5-turbo)
│   ├── Azure Log Analytics Workspace
│   ├── Azure Application Insights
│   └── Azure App Service Plan
└── usecase.bicep (Use case specific resources)
    ├── Azure Web App
    ├── Azure Bot Service
    ├── App Settings Configuration
    └── Role Assignment (Managed Identity → OpenAI)
```

## Module Details

### main.bicep

The main orchestration template that:
- Creates a resource group
- Deploys platform resources
- Deploys use case specific resources with dependencies on platform outputs

### platform.bicep

Contains platform-level resources that can be shared across multiple use cases:

| Resource Type | Purpose | Resource Name Format |
|---------------|---------|----------------------|
| Azure OpenAI Service | AI/ML platform service | `oai-{resourceToken}` |
| OpenAI Model Deployment | GPT-3.5-turbo deployment | `gpt-35-turbo` |
| Log Analytics Workspace | Centralized logging | `log-{resourceToken}` |
| Application Insights | Application monitoring | `appi-{resourceToken}` |
| App Service Plan | Compute platform | `plan-{resourceToken}` |

**Outputs:**
- `openAiServiceId` - Resource ID of OpenAI service
- `openAiServiceName` - Name of OpenAI service
- `appInsightsInstrumentationKey` - Instrumentation key for App Insights
- `appInsightsConnectionString` - Connection string for App Insights
- `appServicePlanId` - Resource ID of App Service Plan

### usecase.bicep

Contains application-specific resources for the bot use case:

| Resource Type | Purpose | Resource Name Format |
|---------------|---------|----------------------|
| Azure Web App | Bot backend application | `web-{resourceToken}` |
| Azure Bot Service | Bot service registration | `bot-{resourceToken}` |
| App Settings | Web App configuration | N/A |
| Role Assignment | OpenAI access permission | N/A |

**Dependencies:**
- Requires `appServicePlanId` from platform module
- Requires `appInsightsInstrumentationKey` from platform module

**Outputs:**
- `webAppName` - Name of the web app
- `webAppHostName` - Default hostname of the web app
- `webAppPrincipalId` - Managed identity principal ID
- `botServiceName` - Name of the bot service

### role.bicep

A reusable module for creating role assignments:
- Used by usecase.bicep to grant OpenAI access to the Web App's managed identity
- Role: "Cognitive Services OpenAI User" (5e0bd9bd-7b93-4f28-af87-19fc36ad61bd)

## Deployment

### Prerequisites
- Azure CLI installed
- Bicep CLI installed
- Appropriate Azure permissions

### Deploy Infrastructure

```bash
# Set parameters
ENVIRONMENT_NAME="your-environment-name"
LOCATION="japaneast"

# Deploy
az deployment sub create \
  --location $LOCATION \
  --template-file infra/main.bicep \
  --parameters environmentName=$ENVIRONMENT_NAME location=$LOCATION
```

### Validate Bicep Files

```bash
# Validate all bicep files
bicep build infra/main.bicep
bicep build infra/platform.bicep
bicep build infra/usecase.bicep
bicep build infra/role.bicep
```

## Benefits of This Structure

1. **Reusability**: Platform resources can be shared across multiple use cases
2. **Independent Lifecycle Management**: Platform and use case resources can be updated independently
3. **Clear Separation of Concerns**: Infrastructure layers are well-defined
4. **Scalability**: Easy to add new use cases that leverage the same platform resources
5. **Cost Management**: Platform resources can be tagged and managed separately from use case resources

## Migration from Previous Structure

The previous implementation had all resources defined in `therest.bicep`. This has been refactored into:
- `platform.bicep` - Platform resources
- `usecase.bicep` - Use case specific resources

The `therest.bicep` file is kept for backward compatibility but is no longer used by `main.bicep`.

## Future Enhancements

- Consider adding separate parameter files for different environments
- Add Azure Key Vault for secret management
- Implement network security with VNet integration
- Add monitoring and alerting rules
- Consider using Bicep Registry modules for common patterns
