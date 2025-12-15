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

Create a staging-specific Entra app registration for the bot.

You can create a staging bot identity by running `pwsh -File ./infra/hooks/preprovision.ps1` with `AZD_ENV_NAME=staging`, then use the generated `microsoftAppId`/`microsoftAppPassword` as the Staging environment secrets.

### Step 2: Add a Staging Federated Credential (OIDC)

If you're reusing the same OIDC app registration as Production, you do **not** need a new OIDC app registration for staging.
You only need to add a federated credential for the **Staging** GitHub Environment on that existing OIDC app registration (skip this step if it's already configured).

Key point: the federated credential subject must include `environment:Staging`.

```bash
export GITHUB_ORG="<your-org>"
export GITHUB_REPO="<your-repo>"
export CLIENT_ID="<staging-oidc-client-id>" # same value as your repository secret AZURE_CLIENT_ID (the Production OIDC app registration client ID)

az ad app federated-credential create \
  --id $CLIENT_ID \
  --parameters '{
    "name": "github-actions-staging",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:'"$GITHUB_ORG"'/'"$GITHUB_REPO"':environment:Staging",
    "audiences": ["api://AzureADTokenExchange"],
    "description": "GitHub Actions OIDC for Staging environment"
  }'
```

### Step 3: Create GitHub Environment

First, create the GitHub environment that will be used for staging deployments:

1. Go to your repository on GitHub
2. Navigate to Settings → Environments
3. Click "New environment"
4. Name it **exactly** `Staging` (must match workflow configuration)
5. Optionally configure protection rules (e.g., require approval)

### Step 4: Configure GitHub Secrets

Set the following secrets in your GitHub repository (Settings → Secrets and variables → Actions):

**Repository-level secrets** (Settings → Secrets and variables → Actions → Repository secrets):
- `AZURE_CLIENT_ID`: Client ID from Step 2
- `AZURE_TENANT_ID`: Your Azure tenant ID (same for all environments)
- `AZURE_SUBSCRIPTION_ID`: Your Azure subscription ID (same for all environments)

**Environment-level secrets** (Settings → Secrets and variables → Actions → Environment secrets → Staging):
- `BOT_APP_ID`: microsoftAppId value
- `BOT_APP_PASSWORD`: microsoftAppPassword value

Important: to ensure the workflow uses the repository-scoped Azure secrets, do not define `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, or `AZURE_SUBSCRIPTION_ID` as **Staging environment** secrets.

You can set secrets using the GitHub CLI:

```bash
# Repository-level secrets
gh secret set AZURE_CLIENT_ID --body "<client-id>"
gh secret set AZURE_TENANT_ID --body "<tenant-id>"
gh secret set AZURE_SUBSCRIPTION_ID --body "<subscription-id>"

# Environment-level secrets (requires the Staging environment to exist first)
gh secret set BOT_APP_ID --env Staging --body "<bot-app-id>"
gh secret set BOT_APP_PASSWORD --env Staging --body "<bot-app-password>"
```

### Step 5: Grant RBAC Permissions (CRITICAL)

**This is the most common cause of deployment failures.** The staging OIDC service principal needs permissions to create and manage Azure resources.

#### Required Permissions

The service principal must have **Contributor** role assigned at either:
- **Subscription level** (simpler, broader permissions), OR
- **Resource group level** (more restrictive, but requires pre-creating the resource group)

#### Option A: Subscription-Level Contributor (Recommended for Initial Setup)

This allows the service principal to create resource groups and manage all resources within the subscription:

```bash
# Get the service principal object ID
export CLIENT_ID="<your-oidc-client-id>"  # Same as AZURE_CLIENT_ID secret
export AZURE_SUBSCRIPTION_ID="<your-subscription-id>"
export SP_OBJECT_ID=$(az ad sp show --id $CLIENT_ID --query id --output tsv)

# Assign Contributor role at subscription level
az role assignment create \
  --role Contributor \
  --assignee-object-id $SP_OBJECT_ID \
  --assignee-principal-type ServicePrincipal \
  --scope /subscriptions/$AZURE_SUBSCRIPTION_ID

# Verify the role assignment
az role assignment list \
  --assignee $SP_OBJECT_ID \
  --scope /subscriptions/$AZURE_SUBSCRIPTION_ID \
  --query "[].{Role:roleDefinitionName,Scope:scope}" \
  --output table
```

#### Option B: Resource Group-Level Contributor (More Restrictive)

If you prefer to limit permissions to a specific resource group:

```bash
# Pre-create the resource group (required for resource-group-level RBAC)
export RG_NAME="rg-systempromptbot-staging"
export LOCATION="japaneast"

az group create --name $RG_NAME --location $LOCATION

# Assign Contributor role at resource group level
az role assignment create \
  --role Contributor \
  --assignee-object-id $SP_OBJECT_ID \
  --assignee-principal-type ServicePrincipal \
  --scope /subscriptions/$AZURE_SUBSCRIPTION_ID/resourceGroups/$RG_NAME

# Verify the role assignment
az role assignment list \
  --assignee $SP_OBJECT_ID \
  --scope /subscriptions/$AZURE_SUBSCRIPTION_ID/resourceGroups/$RG_NAME \
  --query "[].{Role:roleDefinitionName,Scope:scope}" \
  --output table
```

#### Verifying Permissions

After assigning the role, verify that the service principal can access the subscription:

```bash
# Test authentication and permissions using the same OIDC method as CI
# This requires setting up federated credentials locally (advanced)
# Alternatively, test with the Azure Portal:
# 1. Go to Subscriptions → <your-subscription> → Access control (IAM) → Role assignments
# 2. Search for the service principal by its Object ID or Client ID
# 3. Verify "Contributor" role is assigned

# From CLI, verify the service principal exists and check its permissions
az ad sp show --id $CLIENT_ID --query "{DisplayName:displayName,ObjectId:id,AppId:appId}" --output table

echo "Checking role assignments for service principal..."
az role assignment list --assignee $SP_OBJECT_ID --all --output table
```

**Common Issues:**
- ❌ **403 Forbidden during provision**: Service principal lacks Contributor role
- ❌ **"ERROR CODE UNAVAILABLE"**: Often indicates RBAC permissions missing
- ❌ Role assignment exists but still fails: Allow 5-10 minutes for RBAC propagation after assignment

## How It Works

Once configured, the staging deployment workflow (`.github/workflows/staging-deploy.yml`) will:

1. **Trigger** on push to `main` when `app/**` or `infra/**` files change
2. **Authenticate** to Azure using OIDC (passwordless)
3. **Configure** azd environment with staging parameters (including `AZURE_RESOURCE_GROUP=rg-systempromptbot-staging`)
4. **Provision** infrastructure via `azd provision` (creates/updates Azure resources in the `rg-systempromptbot-staging` resource group)
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

- **Resource group**: Staging uses a separate resource group (`rg-systempromptbot-staging`)
- **Bot identity**: Staging has its own Entra app registration
- **OIDC credentials**: Staging uses separate OIDC app registration
- **Deployment trigger**: Automatic on code push (production may require manual approval)
- **Azure location**: Both use `japaneast` by default (configurable in workflow)

## Troubleshooting

### 403 Forbidden Error During Provision (MOST COMMON)

**Symptom**: Workflow fails at "Provision infrastructure" step with:
```
ERROR: initializing provisioning manager: checking if resource group exists: checking resource existence by id: HEAD https://management.azure.com/subscriptions/***/resourceGroups/rg-systempromptbot-staging
RESPONSE 403: 403 Forbidden
ERROR CODE UNAVAILABLE
```

**Root Cause**: The OIDC service principal lacks RBAC permissions to read or create the resource group.

**Solution**:
1. Verify the service principal has Contributor role assigned (see Step 5 above)
2. Check the role assignment:
   ```bash
   export CLIENT_ID="<your-oidc-client-id>"
   export SP_OBJECT_ID=$(az ad sp show --id $CLIENT_ID --query id --output tsv)
   
   # List all role assignments for this service principal
   az role assignment list --assignee $SP_OBJECT_ID --all --output table
   ```
3. If no Contributor role is found, assign it:
   ```bash
   # Subscription-level (recommended)
   az role assignment create \
     --role Contributor \
     --assignee-object-id $SP_OBJECT_ID \
     --assignee-principal-type ServicePrincipal \
     --scope /subscriptions/$AZURE_SUBSCRIPTION_ID
   ```
4. Wait 5-10 minutes for RBAC propagation, then re-run the workflow
5. The new "Validate Azure RBAC permissions" step in the workflow will help diagnose this issue

### Workflow fails with authentication error
- Verify OIDC federated credential is configured for the `Staging` GitHub Environment (subject includes `environment:Staging`)
- Check that all GitHub secrets are set correctly (`AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`)
- Ensure the OIDC app registration (service principal) exists and hasn't been deleted
- Verify the federated credential hasn't been accidentally removed

### Bot identity errors during provision
- Verify `BOT_APP_ID` and `BOT_APP_PASSWORD` secrets are set in the Staging environment (not repository-level)
- Ensure the bot identity still exists in Entra ID (not deleted)
- Check that client secret has not expired (check expiration in Azure Portal → App registrations)

### Resource group or resources not found
- First run will create the resource group automatically (requires Contributor role)
- Ensure service principal has permissions to create resource groups
- Check Azure subscription quota limits
- Verify the `AZURE_RESOURCE_GROUP` environment variable is set correctly in the workflow

### azd provision fails with other errors
- Review workflow logs for specific error messages
- Verify Bicep files are valid (bicep-validation.yml workflow should pass)
- Check that all required parameters are provided in the "Configure azd environment" step
- Ensure Azure OpenAI is available in the target location (`japaneast`)

### "no default response for prompt 'Pick a resource group to use'"
- This error occurs when `AZURE_RESOURCE_GROUP` is not set in the azd environment
- The workflow now sets this automatically to `rg-systempromptbot-staging`
- If using a custom resource group name, update the `azd env set AZURE_RESOURCE_GROUP` line in the workflow

### RBAC changes not taking effect
- Allow 5-10 minutes for RBAC propagation after making role assignments
- Clear any cached credentials: `az account clear` (locally)
- In GitHub Actions, OIDC tokens are fresh each run, so no cache clearing needed
- Verify the role assignment is at the correct scope (subscription or resource group)

## Maintenance

- **Client secret expiration**: Bot identity client secrets expire based on tenant policy. Rotate and update `BOT_APP_PASSWORD` secret in the Staging environment before expiration.
- **OIDC credential**: Federated credentials don't expire but should be reviewed periodically
- **Resource cleanup**: Consider adding auto-deletion for old staging resources if needed

## References

- [Azure OIDC Setup Guide](./azure-oidc-setup.md)
- [Azure Developer CLI (azd) Documentation](https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/)
- [GitHub Actions Environments](https://docs.github.com/en/actions/deployment/targeting-different-environments/using-environments-for-deployment)
