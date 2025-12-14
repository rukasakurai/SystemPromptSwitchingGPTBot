# Azure OIDC Setup for GitHub Actions

## Required GitHub Secrets

Configure these three secrets in your repository settings:

- `AZURE_CLIENT_ID` - Application (client) ID of your Azure AD app registration
- `AZURE_TENANT_ID` - Directory (tenant) ID of your Azure AD
- `AZURE_SUBSCRIPTION_ID` - Azure subscription ID

## Configure Federated Credentials

Follow the [Azure/login OIDC guide](https://github.com/azure/login#configure-a-service-principal-with-a-federated-credential-to-use-oidc-based-authentication) to set up federated credentials for your service principal.

Key settings for this repository:
- **Entity type**: Branch
- **GitHub branch name**: `main`
- **Name**: `github-actions-main`
