# Fix Instructions for 403 Forbidden Error in Staging Deployment

## Problem Summary

The staging deployment workflow is failing with a **403 Forbidden** error when trying to provision Azure resources. This occurs because the OIDC service principal used for authentication lacks the necessary RBAC (Role-Based Access Control) permissions.

**Error message:**
```
ERROR: initializing provisioning manager: checking if resource group exists: 
checking resource existence by id: HEAD https://management.azure.com/subscriptions/***/resourceGroups/rg-systempromptbot-staging
RESPONSE 403: 403 Forbidden
ERROR CODE UNAVAILABLE
```

## Root Cause

The service principal authenticated via OIDC (identified by the `AZURE_CLIENT_ID` secret) does not have the **Contributor** role assigned at either:
- Subscription level, OR
- Resource group level (`rg-systempromptbot-staging`)

Without this role, the service principal cannot:
- Check if the resource group exists
- Create the resource group if it doesn't exist
- Provision and manage Azure resources

## Solution: Assign Contributor Role

You must assign the **Contributor** role to the service principal. Choose one of the options below:

### Option A: Subscription-Level Contributor (Recommended)

This gives the service principal permissions to create and manage resources across the entire subscription:

```bash
# Set your values
export CLIENT_ID="<your-AZURE_CLIENT_ID>"  # From GitHub secret
export AZURE_SUBSCRIPTION_ID="<your-subscription-id>"  # From GitHub secret

# Get the service principal object ID
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
  --output table
```

### Option B: Resource Group-Level Contributor (More Restrictive)

This limits the service principal's permissions to only the staging resource group:

```bash
# Set your values
export CLIENT_ID="<your-AZURE_CLIENT_ID>"
export AZURE_SUBSCRIPTION_ID="<your-subscription-id>"
export RG_NAME="rg-systempromptbot-staging"
export LOCATION="japaneast"

# Get the service principal object ID
export SP_OBJECT_ID=$(az ad sp show --id $CLIENT_ID --query id --output tsv)

# Pre-create the resource group (required for resource-group-level RBAC)
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
  --output table
```

## Validation

### Before Re-running the Workflow

Use the validation script to check your RBAC setup:

```bash
bash docs/validate-staging-rbac.sh
```

This script will:
- ✓ Verify the service principal exists
- ✓ Check role assignments (Contributor/Owner)
- ✓ Validate federated credentials for Staging environment
- ✓ Confirm resource group status
- ✓ Provide step-by-step guidance if issues are found

### Re-run the Workflow

Once RBAC is configured:

1. **Wait 5-10 minutes** for Azure RBAC changes to propagate
2. Go to your repository's Actions tab
3. Select the "Staging - Deploy with azd" workflow
4. Click "Run workflow"
5. Select the `main` branch
6. Click "Run workflow"

The workflow now includes a "Validate Azure RBAC permissions" step that will help diagnose permission issues before attempting to provision.

## Improvements Made in This PR

### 1. Pre-Flight RBAC Validation in CI
- Added validation step in `.github/workflows/staging-deploy.yml`
- Checks service principal permissions before provisioning
- Provides actionable warnings if permissions are missing
- References documentation for troubleshooting

### 2. Local Validation Tool
- Created `docs/validate-staging-rbac.sh` script
- Validates RBAC setup locally before running CI
- Checks all prerequisites (service principal, roles, federated credentials)
- Provides detailed diagnostics and fix instructions

### 3. Enhanced Documentation
- Updated `docs/staging-setup.md` with:
  - Prominent RBAC setup instructions (marked as CRITICAL)
  - Detailed 403 Forbidden troubleshooting section
  - Verification commands
  - Common issues and solutions

## Important Notes

### RBAC Propagation Delay
- Azure RBAC changes can take **5-10 minutes to propagate**
- If the workflow still fails immediately after assigning the role, wait and try again

### Where to Find Your Client ID
- The `AZURE_CLIENT_ID` is stored as a GitHub repository secret
- Go to: Settings → Secrets and variables → Actions → Repository secrets
- Look for `AZURE_CLIENT_ID`
- This is the Client ID (Application ID) of your OIDC app registration

### Verify Service Principal Exists
```bash
# Check if service principal exists
az ad sp show --id <your-AZURE_CLIENT_ID> --query "{DisplayName:displayName,ObjectId:id,AppId:appId}" --output table
```

If this command fails, the service principal doesn't exist, which means:
- The OIDC app registration might not exist, OR
- The app registration exists but hasn't been used yet (service principal is created on first use)

## Need Help?

### Check the Workflow Logs
The new validation step will provide detailed diagnostic information in the workflow logs. Look for:
- "Validate Azure RBAC permissions" step
- Error messages with `::error::` or `::warning::` prefixes
- Role assignment output

### Run the Local Validation Script
```bash
bash docs/validate-staging-rbac.sh
```

This will guide you through the validation process and provide specific commands to fix any issues found.

### Review the Documentation
- **Staging Setup Guide**: `docs/staging-setup.md`
- **OIDC Setup Guide**: `docs/azure-oidc-setup.md` (if referenced)

### Common Issues

**Q: I assigned the role but it still fails**
- Wait 5-10 minutes for RBAC propagation
- Verify the role assignment: `az role assignment list --assignee <SP_OBJECT_ID> --all --output table`

**Q: How do I find my service principal Object ID?**
```bash
az ad sp show --id <AZURE_CLIENT_ID> --query id --output tsv
```

**Q: Can I use a different role instead of Contributor?**
- **Owner** role will also work (has all Contributor permissions plus more)
- **Reader** role is NOT sufficient (read-only, cannot create/modify resources)
- Custom roles work if they include: `Microsoft.Resources/subscriptions/resourceGroups/*`

## Summary

✅ **What you need to do:**
1. Assign Contributor role to the service principal (use commands above)
2. Wait 5-10 minutes for propagation
3. Run validation script: `bash docs/validate-staging-rbac.sh`
4. Re-run the GitHub Actions workflow

✅ **What this PR provides:**
- Better error messages in CI
- Local validation tool
- Improved documentation

This is a **configuration issue**, not a code bug. Once RBAC is properly configured, the deployment will succeed.
