# GitHub Copilot Coding Agent with Azure Access

This document explains how to configure GitHub Copilot Coding Agent to securely access Azure resources in this repository using the Azure Developer CLI (azd) extension.

## Prerequisites

- Azure Developer CLI (azd) installed locally (latest version)
- Azure CLI (az) installed (only required if you use the optional identity commands below)
- Azure subscription with permissions to create resource groups and managed identities
- GitHub repository cloned locally
- GitHub Copilot subscription enabled
- Repository admin permissions (required to create/update the `copilot` GitHub environment and Copilot coding agent settings)
- A configured Git remote for the repository (required so federated credentials can be created)

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

### 2. Check for Existing or Create User-Assigned Managed Identity (Optional)

The `azd coding-agent config` command can create a user-assigned managed identity automatically, but you may want to check for existing identities or create one manually beforehand.

#### Check for Existing User-Assigned Managed Identities

To list all user-assigned managed identities in your subscription:

```bash
# List all user-assigned managed identities in the current subscription
az identity list --query "[].{Name:name, ResourceGroup:resourceGroup, Location:location, ClientId:clientId}" -o table

# List identities in a specific resource group (shows all default columns)
az identity list --resource-group <resource-group-name> -o table
```

#### Create a User-Assigned Managed Identity Manually

If you prefer to create the managed identity before running the configuration:

```bash
# Create a resource group (if needed)
az group create --name <resource-group-name> --location <location>

# Create a user-assigned managed identity
az identity create --name <identity-name> --resource-group <resource-group-name>

# Get the identity details (including Client ID)
az identity show --name <identity-name> --resource-group <resource-group-name> --query "{Name:name, ClientId:clientId, PrincipalId:principalId}" -o json
```

You can then select this identity during the `azd coding-agent config` interactive setup.

### 3. Run the Configuration Command

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
6. Add or update the GitHub Actions workflow `.github/workflows/copilot-setup-steps.yml` (and related assets)
7. Configure GitHub environment variables

### 4. Configure GitHub Copilot Settings

After running the configuration, you'll need to add the Azure MCP Server configuration to your GitHub Copilot coding agent settings (the command output and/or the generated pull request includes the exact JSON snippet to paste):

1. Navigate to your GitHub repository settings
2. Go to **Copilot** → **Coding agent**
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

**Note**: The `"tools": ["*"]` configuration grants access to all available Azure MCP Server tools. This is the recommended configuration for the Copilot Coding Agent to interact with Azure resources. Access is constrained by the RBAC role (default: Reader) assigned to the managed identity.

### 5. Merge Pull Request (if created)

The `azd coding-agent config` command creates a branch and pull request containing the workflow setup (typically on a branch named `azd-enable-copilot-coding-agent-with-azure`). Review and merge it to enable Copilot access.

### 6. Verify Configuration

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
