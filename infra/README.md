# Infrastructure as Code

This directory contains Azure Bicep templates for deploying the SystemPromptSwitchingGPTBot infrastructure.

## Structure

- **main.bicep** - Main orchestration template (subscription-level deployment)
- **platform.bicep** - Platform resources (Azure OpenAI, App Service Plan, Log Analytics, App Insights)
- **usecase.bicep** - Use case specific resources (Web App, Bot Service, Role Assignments)
- **role.bicep** - Reusable role assignment module
- **main.parameters.json** - Parameters file for main.bicep
- **therest.bicep** - Legacy template (deprecated, kept for reference)

## Quick Start

```bash
# Deploy to Azure
az deployment sub create \
  --location japaneast \
  --template-file main.bicep \
  --parameters environmentName=mybot location=japaneast
```

## Resource Separation

### Platform Resources (platform.bicep)
These are foundational resources that can be reused across multiple use cases:
- Azure OpenAI Service
- Azure Log Analytics Workspace
- Azure Application Insights
- Azure App Service Plan

### Use Case Resources (usecase.bicep)
These are specific to the bot application:
- Azure Web App
- Azure Bot Service
- App configuration and role assignments

For detailed documentation, see [docs/infrastructure.md](../docs/infrastructure.md).
