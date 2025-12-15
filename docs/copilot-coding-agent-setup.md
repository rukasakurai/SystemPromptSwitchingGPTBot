# GitHub Copilot Coding Agent with Azure Access

This document explains how to configure GitHub Copilot Coding Agent to securely access Azure resources in this repository using the Azure Developer CLI (azd) extension.

## Prerequisites

- Azure Developer CLI (azd) installed locally (latest version)
- Azure subscription with permissions to create resource groups and managed identities
- GitHub repository cloned locally
- GitHub Copilot subscription enabled
- Repository permissions to update workflows and GitHub environment settings

## Overview

The Azure Developer CLI Copilot Coding Agent extension (`azure.coding-agent`) enables GitHub Copilot Coding Agent to interact with Azure resources securely using:
- **Federated credentials** (OpenID Connect) - no secrets stored in GitHub
- **User-assigned managed identity** - for secure Azure access
- **RBAC role assignments** - least privilege access (Reader by default)

## Configuration Steps

### 1. Install the Azure Coding Agent Extension

```bash
# Install the extension
azd extension install azure.coding-agent

# Verify installation
azd extension list --installed
```

You should see `azure.coding-agent` in the installed extensions list.

### 2. Run the Configuration Command

From your local repository root:

```bash
azd coding-agent config
```

This interactive command will:
1. Prompt you to select an Azure subscription
2. Create or select a user-assigned managed identity
3. Create or select a resource group
4. Configure RBAC role assignment (Reader by default)
5. Set up federated credentials between GitHub and Azure
6. Create or update `.github/workflows/copilot-agent-azure.yml`
7. Configure GitHub environment variables

### 3. Configure GitHub Copilot Settings

After running the configuration, you'll need to add the Azure MCP Server configuration to your GitHub Copilot coding agent settings:

1. Navigate to your GitHub repository settings
2. Go to **Copilot** → **Coding Agent** settings
3. Add the following MCP server configuration:

```json
{
  "mcpServers": {
    "Azure": {
      "type": "local",
      "command": "npx",
      "args": ["-y", "@azure/mcp@latest", "server", "start"],
      "tools": ["*"]
    }
  }
}
```

### 4. Merge Pull Request (if created)

The `azd coding-agent config` command may create a pull request with the new workflow file. Review and merge it to enable Copilot access.

### 5. Verify Configuration

The configuration creates a GitHub environment (typically named `copilot`) with three variables:
- `AZURE_CLIENT_ID` - Managed identity client ID
- `AZURE_SUBSCRIPTION_ID` - Azure subscription ID
- `AZURE_TENANT_ID` - Azure tenant ID

Verify these are set correctly in your repository's environment settings.

## Security Model

- **No secrets in GitHub**: Uses OpenID Connect federated credentials
- **Managed Identity**: Azure resources are accessed via managed identity
- **RBAC**: Default Reader role provides read-only access (configurable during setup)
- **Least Privilege**: Only grants necessary permissions for Copilot operations

## Updating Configuration

To modify the configuration (e.g., change RBAC role, resource group):

```bash
# Run the configuration command again
azd coding-agent config
```

Select different options when prompted to update the configuration.

## Troubleshooting

### Extension not found
```bash
# Upgrade azd to the latest version
azd upgrade

# Reinstall the extension
azd extension install azure.coding-agent
```

### Permissions issues
Ensure you have:
- Azure subscription Contributor role (or Owner)
- GitHub repository admin access
- Permissions to create managed identities and resource groups

### GitHub environment not created
Manually create the `copilot` environment in GitHub repository settings and add the three required variables from the azd output.

## References

- [Azure Developer CLI Copilot Coding Agent Extension](https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/extensions/copilot-coding-agent-extension)
- [Extension Announcement and Best Practices](https://devblogs.microsoft.com/azure-sdk/azure-developer-cli-copilot-coding-agent-config/)
- [Azure MCP Server Documentation](https://learn.microsoft.com/en-us/azure/developer/azure-mcp-server/how-to/github-copilot-coding-agent)
