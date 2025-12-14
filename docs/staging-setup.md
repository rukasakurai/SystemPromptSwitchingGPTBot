# Staging Environment Setup

This document describes how to set up the staging Azure environment and configure the CI/CD pipeline for automatic deployments.

## Overview

The staging environment is automatically deployed via GitHub Actions whenever code changes are pushed to the `main` branch (in `app/**` or `infra/**` directories). The workflow runs the equivalent of `azd up` to provision infrastructure and deploy the application.

## Prerequisites

1. Azure subscription with appropriate permissions
2. Azure CLI and azd CLI (for local setup)
3. GitHub repository access with admin permissions
4. PowerShell (for running preprovision script)

## One-Time Setup

### Step 1: Create Bot Identity for Staging

The staging environment needs its own bot identity (Entra app registration) separate from production. Run the preprovision script to create one:

```bash
# Set the environment name for staging
export AZD_ENV_NAME=staging

# Run the preprovision script
pwsh -File ./infra/hooks/preprovision.ps1
```

This will:
- Create a new Entra app registration
- Create a client secret for the bot
- Store the values in azd environment

After running the script, retrieve the values for GitHub secrets:

```bash
azd env get-values | grep microsoft
```

You'll see output like:
```
microsoftAppId=<app-id>
microsoftAppPassword=<password>
microsoftAppTenantId=<tenant-id>
microsoftAppType=SingleTenant
```

**Save these values** - you'll need them for GitHub secrets.

### Step 2: Set Up OIDC Authentication for GitHub Actions (Staging)

Follow the instructions in [azure-oidc-setup.md](./azure-oidc-setup.md) to create a separate OIDC app registration for staging deployments.

Key differences for staging:
- Use a separate app registration (e.g., `github-actions-staging-oidc`)
- Configure federated credential for the **staging** environment:

```bash
export GITHUB_ORG="<your-org>"
export GITHUB_REPO="<your-repo>"
export CLIENT_ID="<staging-oidc-client-id>"

az ad app federated-credential create \
  --id $CLIENT_ID \
  --parameters '{
    "name": "github-actions-staging",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:'"$GITHUB_ORG"'/'"$GITHUB_REPO"':environment:staging",
    "audiences": ["api://AzureADTokenExchange"],
    "description": "GitHub Actions OIDC for staging environment"
  }'
```

### Step 3: Configure GitHub Secrets

Set the following secrets in your GitHub repository (Settings → Secrets and variables → Actions):

**Staging OIDC credentials:**
- `AZURE_CLIENT_ID_STAGING`: Client ID from Step 2
- `AZURE_TENANT_ID`: Your Azure tenant ID (same for all environments)
- `AZURE_SUBSCRIPTION_ID`: Your Azure subscription ID (same for all environments)

**Staging bot identity (from Step 1):**
- `STAGING_BOT_APP_ID`: microsoftAppId value
- `STAGING_BOT_APP_PASSWORD`: microsoftAppPassword value

You can set secrets using the GitHub CLI:

```bash
gh secret set AZURE_CLIENT_ID_STAGING --body "<client-id>"
gh secret set AZURE_TENANT_ID --body "<tenant-id>"
gh secret set AZURE_SUBSCRIPTION_ID --body "<subscription-id>"
gh secret set STAGING_BOT_APP_ID --body "<bot-app-id>"
gh secret set STAGING_BOT_APP_PASSWORD --body "<bot-app-password>"
```

### Step 4: Create GitHub Environment

1. Go to your repository on GitHub
2. Navigate to Settings → Environments
3. Click "New environment"
4. Name it **exactly** `staging` (must match workflow configuration)
5. Optionally configure protection rules (e.g., require approval)

### Step 5: Grant RBAC Permissions

The staging OIDC service principal needs permissions to create and manage Azure resources:

```bash
# Get the service principal object ID
export SP_OBJECT_ID=$(az ad sp show --id $CLIENT_ID --query id --output tsv)

# Assign Contributor role to the subscription or specific resource group
az role assignment create \
  --role Contributor \
  --assignee-object-id $SP_OBJECT_ID \
  --assignee-principal-type ServicePrincipal \
  --scope /subscriptions/$AZURE_SUBSCRIPTION_ID
```

If you prefer to scope to a specific resource group (recommended):

```bash
# The workflow will create a resource group named rg-staging (by default)
# You may need to pre-create it or grant permissions after first run
az group create --name rg-staging --location japaneast

az role assignment create \
  --role Contributor \
  --assignee-object-id $SP_OBJECT_ID \
  --assignee-principal-type ServicePrincipal \
  --scope /subscriptions/$AZURE_SUBSCRIPTION_ID/resourceGroups/rg-staging
```

## How It Works

Once configured, the staging deployment workflow (`.github/workflows/staging-deploy.yml`) will:

1. **Trigger** on push to `main` when `app/**` or `infra/**` files change
2. **Authenticate** to Azure using OIDC (passwordless)
3. **Configure** azd environment with staging parameters
4. **Provision** infrastructure via `azd provision` (creates/updates Azure resources)
5. **Deploy** application code via `azd deploy` (builds and deploys to App Service)
6. **Output** deployment information for verification

## Testing the Workflow

You can manually trigger the workflow to test:

1. Go to Actions tab in your repository
2. Select "Staging - Deploy with azd" workflow
3. Click "Run workflow"
4. Select the `main` branch
5. Click "Run workflow"

Monitor the workflow execution to ensure all steps complete successfully.

## Verifying the Deployment

After successful deployment:

1. Check the workflow logs for the bot endpoint URL
2. The staging bot will be accessible at `https://web-<resource-token>.azurewebsites.net/api/messages`
3. Verify resources in Azure Portal under the staging resource group
4. Test the bot using Bot Framework Emulator or Teams (after configuring the channel)

## Differences from Production

- **Resource group**: Staging uses a separate resource group (e.g., `rg-staging`)
- **Bot identity**: Staging has its own Entra app registration
- **OIDC credentials**: Staging uses separate OIDC app registration
- **Deployment trigger**: Automatic on code push (production may require manual approval)
- **Azure location**: Both use `japaneast` by default (configurable in workflow)

## Troubleshooting

### Workflow fails with authentication error
- Verify OIDC federated credential is configured for the `staging` environment
- Check that all GitHub secrets are set correctly
- Ensure service principal has Contributor role

### Bot identity errors during provision
- Verify `STAGING_BOT_APP_ID` and `STAGING_BOT_APP_PASSWORD` secrets are set
- Ensure the bot identity still exists in Entra ID (not deleted)
- Check that client secret has not expired

### Resource group or resources not found
- First run will create the resource group automatically
- Ensure service principal has permissions to create resource groups
- Check Azure subscription quota limits

### azd provision fails
- Review workflow logs for specific error messages
- Verify Bicep files are valid (bicep-validation.yml should pass)
- Check that all required parameters are provided

## Maintenance

- **Client secret expiration**: Bot identity client secrets expire based on tenant policy. Rotate and update `STAGING_BOT_APP_PASSWORD` secret before expiration.
- **OIDC credential**: Federated credentials don't expire but should be reviewed periodically
- **Resource cleanup**: Consider adding auto-deletion for old staging resources if needed

## References

- [Azure OIDC Setup Guide](./azure-oidc-setup.md)
- [Azure Developer CLI (azd) Documentation](https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/)
- [GitHub Actions Environments](https://docs.github.com/en/actions/deployment/targeting-different-environments/using-environments-for-deployment)
