# Azure OpenAI to Foundry Migration with Custom Agent

> [!WARNING]
> **PREVIEW VERSION - UNTESTED**
> 
> This custom agent is in preview and has not been thoroughly tested in production environments.
> Please use with caution:
> - Test in a non-production environment first
> - Review all changes carefully before applying
> - Validate infrastructure changes with `az bicep build` and `az deployment group validate`
> - Keep backups of your original Bicep files
> 
> This tool is provided as-is and will be improved based on community feedback and testing.

This document explains how to use the custom GitHub Copilot agent to migrate from Azure OpenAI (`kind=OpenAI`) to Microsoft Foundry (`kind=AIServices`).

## Overview

Microsoft Foundry (AI Services) is a superset platform that includes Azure OpenAI capabilities plus additional features like:
- Multi-provider model catalog (Meta Llama, Mistral, Cohere, xAI, Microsoft)
- Advanced agent orchestration
- Enhanced evaluation and monitoring tools
- Hybrid deployment options (cloud and on-premises)

The migration is **backward compatible** - all existing Azure OpenAI deployments and endpoints continue to work after migration.

## Prerequisites

- GitHub Copilot subscription enabled
- GitHub Copilot Coding Agent configured (see [copilot-coding-agent-setup.md](./copilot-coding-agent-setup.md))
- Repository cloned locally
- Basic understanding of Bicep infrastructure-as-code

## Using the Custom Agent

### Option 1: Via GitHub Copilot Chat (Recommended)

1. Open GitHub Copilot Chat in your IDE
2. Reference the custom agent prompt:
   ```
   @github use the prompt in .github/prompts/openai-to-foundry-migration.prompt.md to migrate our Azure OpenAI resources to Microsoft Foundry
   ```

3. The agent will:
   - Scan `infra/` directory for Azure OpenAI resources
   - Identify resources with `kind: 'OpenAI'`
   - Show you the proposed changes
   - Update resources to `kind: 'AIServices'`
   - Validate the infrastructure

4. Review the changes and validate:
   ```bash
   # Validate Bicep files
   az bicep build --file infra/main.bicep
   
   # Or use the workflow
   git push origin <branch>
   # Check .github/workflows/bicep-validation.yml results
   ```

### Option 2: Manual Review with Agent Guidance

1. Ask the agent to analyze current state:
   ```
   @github use .github/prompts/openai-to-foundry-migration.prompt.md to analyze our infrastructure and list what needs to be migrated
   ```

2. Request step-by-step guidance:
   ```
   @github guide me through migrating infra/platform.bicep from OpenAI to Foundry
   ```

3. Have the agent review your changes:
   ```
   @github review my changes to ensure the Foundry migration is correct
   ```

## What Gets Changed

The agent modifies only the infrastructure files:

### Before (Azure OpenAI)
```bicep
resource openAiService 'Microsoft.CognitiveServices/accounts@2025-09-01' = {
  name: 'oai-${resourceToken}'
  location: location
  kind: 'OpenAI'  // ← This line changes
  sku: {
    name: 'S0'
  }
  properties: {
    customSubDomainName: 'oai-${resourceToken}'
    disableLocalAuth: true
    publicNetworkAccess: 'Enabled'
  }
}
```

### After (Microsoft Foundry)
```bicep
resource openAiService 'Microsoft.CognitiveServices/accounts@2025-09-01' = {
  name: 'oai-${resourceToken}'
  location: location
  kind: 'AIServices'  // ← Changed from 'OpenAI'
  sku: {
    name: 'S0'
  }
  properties: {
    customSubDomainName: 'oai-${resourceToken}'
    disableLocalAuth: true
    publicNetworkAccess: 'Enabled'
  }
}
```

## What Stays the Same

The agent preserves:
- ✅ All endpoints (no URL changes)
- ✅ Model deployments (GPT-4o, etc.)
- ✅ RBAC role assignments ("Cognitive Services OpenAI User")
- ✅ Managed identity configuration
- ✅ Application settings
- ✅ API compatibility (no code changes needed)

## No Application Code Changes Required

Your C# bot code (`app/` directory) needs **zero changes**:
- Same Azure OpenAI SDK
- Same endpoints and authentication
- Same `OpenAIEndpoint` and `OpenAIDeployment` configuration
- Managed identity authentication works identically

## Validation Steps

After the agent makes changes:

1. **Bicep Validation**:
   ```bash
   cd infra
   az bicep build --file main.bicep
   ```

2. **Deployment Validation** (dry-run):
   ```bash
   az deployment group validate \
     --resource-group <your-rg> \
     --template-file infra/main.bicep \
     --parameters infra/main.parameters.json
   ```

3. **CI Validation**: Push to a branch and check workflows:
   - `.github/workflows/bicep-validation.yml` - Infrastructure validation
   - `.github/workflows/pr-tests.yml` - Application tests

## Deployment

Once validated, deploy using Azure Developer CLI:

```bash
# Deploy the infrastructure with Foundry
azd up
```

Or via Azure CLI:

```bash
az deployment group create \
  --resource-group <your-rg> \
  --template-file infra/main.bicep \
  --parameters infra/main.parameters.json
```

## Rollback

If you need to rollback, simply revert the `kind` property:

```bicep
kind: 'OpenAI'  // Revert from 'AIServices'
```

Then redeploy. All endpoints and configurations remain compatible.

## Benefits After Migration

Once migrated to Foundry, you can:

1. **Access More Models**: Deploy models from Meta, Mistral, Cohere, xAI, Microsoft
2. **Build AI Agents**: Use advanced orchestration capabilities
3. **Enhanced Monitoring**: Better evaluation and monitoring tools
4. **Hybrid Deployment**: Option to deploy on-premises or at the edge
5. **Unified API**: Switch between model providers without changing code

## Troubleshooting

### Agent doesn't find resources
- Ensure you're in the repository root directory
- Check that `infra/platform.bicep` exists and contains Azure OpenAI resources

### Validation fails after migration
- Verify API version is `2025-09-01` or later
- Check that all properties are preserved
- Review error messages from `az bicep build`

### Application doesn't connect after deployment
- Verify endpoints haven't changed in Application Settings
- Check managed identity still has "Cognitive Services OpenAI User" role
- Review Application Insights for connection errors

## References

- [Custom Agent Prompt](../.github/prompts/openai-to-foundry-migration.prompt.md)
- [Upgrade from Azure OpenAI to Microsoft Foundry](https://learn.microsoft.com/en-us/azure/ai-foundry/how-to/upgrade-azure-openai)
- [Microsoft Foundry Documentation](https://learn.microsoft.com/en-us/azure/ai-foundry/)
- [Azure Cognitive Services Templates](https://learn.microsoft.com/en-us/azure/templates/microsoft.cognitiveservices/accounts)

## Support

For issues or questions:
1. Check the [AGENTS.md](../AGENTS.md) collaboration contract
2. Review Application Insights for runtime issues
3. Consult Microsoft Foundry documentation
