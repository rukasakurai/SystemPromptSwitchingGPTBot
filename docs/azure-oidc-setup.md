# Azure OIDC Setup for GitHub Actions

## Prerequisites

- Azure CLI installed (`az --version` to verify)
- Logged in to Azure (`az login`)
- GitHub CLI installed (`gh --version` to verify)
- Logged in to GitHub (`gh auth login`)

## Step 1: Get Your Azure Information

```bash
# Get your subscription ID
export SUBSCRIPTION_ID=$(az account show --query id --output tsv)
echo "Subscription ID: $SUBSCRIPTION_ID"

# Get your tenant ID
export TENANT_ID=$(az account show --query tenantId --output tsv)
echo "Tenant ID: $TENANT_ID"
```

## Step 2: Create or Use Existing Azure AD App Registration

### Option A: Create a new app registration

```bash
# Set your app name
export APP_NAME="github-actions-oidc"

# Create the app registration
az ad app create --display-name $APP_NAME

# Get the app (client) ID
export CLIENT_ID=$(az ad app list --display-name $APP_NAME --query [0].appId --output tsv)
echo "Client ID: $CLIENT_ID"

# Create a service principal for the app
az ad sp create --id $CLIENT_ID
```

### Option B: Use existing app registration

```bash
# If you already have an app, get its client ID
export CLIENT_ID="<your-existing-client-id>"
```

## Step 3: Assign Azure Permissions

```bash
# Get the service principal object ID
export SP_OBJECT_ID=$(az ad sp show --id $CLIENT_ID --query id --output tsv)

# Assign Contributor role to the subscription
# (adjust role as needed: Contributor, Reader, etc.)
az role assignment create \
  --role Contributor \
  --assignee-object-id $SP_OBJECT_ID \
  --assignee-principal-type ServicePrincipal \
  --scope /subscriptions/$SUBSCRIPTION_ID
```

## Step 4: Configure Federated Credentials

```bash
# Set your GitHub repository details
export GITHUB_ORG="rukasakurai"
export GITHUB_REPO="SystemPromptSwitchingGPTBot"

# Create federated credential for main branch
az ad app federated-credential create \
  --id $CLIENT_ID \
  --parameters '{
    "name": "github-actions-main",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:'"$GITHUB_ORG"'/'"$GITHUB_REPO"':ref:refs/heads/main",
    "audiences": ["api://AzureADTokenExchange"],
    "description": "GitHub Actions OIDC for main branch"
  }'

# Optional: Create federated credential for pull requests
az ad app federated-credential create \
  --id $CLIENT_ID \
  --parameters '{
    "name": "github-actions-pr",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:'"$GITHUB_ORG"'/'"$GITHUB_REPO"':pull_request",
    "audiences": ["api://AzureADTokenExchange"],
    "description": "GitHub Actions OIDC for pull requests"
  }'
```

## Step 5: Configure GitHub Secrets

```bash
# Navigate to your repository directory or specify the repo
cd /path/to/your/repo  # or use -R flag with gh secret set

# Set the three required secrets
gh secret set AZURE_CLIENT_ID --body "$CLIENT_ID"
gh secret set AZURE_TENANT_ID --body "$TENANT_ID"
gh secret set AZURE_SUBSCRIPTION_ID --body "$SUBSCRIPTION_ID"

# Verify secrets were set
gh secret list
```

## Step 6: Verify Configuration

```bash
# List federated credentials to verify
az ad app federated-credential list --id $CLIENT_ID --query "[].{Name:name, Subject:subject}" --output table

# Verify role assignments
az role assignment list --assignee $CLIENT_ID --query "[].{Role:roleDefinitionName, Scope:scope}" --output table
```

## Troubleshooting

If the workflow still fails:

1. **Check federated credential subject**: Ensure it matches your repository and branch exactly
   ```bash
   az ad app federated-credential list --id $CLIENT_ID
   ```

2. **Verify role assignments**: Ensure the service principal has appropriate permissions
   ```bash
   az role assignment list --assignee $CLIENT_ID
   ```

3. **Check GitHub secrets**: Ensure all three secrets are set correctly
   ```bash
   gh secret list
   ```

4. **Review workflow logs**: Check for specific error messages in GitHub Actions

## References

- [Azure/login OIDC guide](https://github.com/azure/login#configure-a-service-principal-with-a-federated-credential-to-use-oidc-based-authentication)
- [Azure Workload Identity Federation](https://learn.microsoft.com/en-us/azure/active-directory/workload-identities/workload-identity-federation)
