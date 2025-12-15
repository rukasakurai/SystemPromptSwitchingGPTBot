#!/bin/bash
# Validation script for staging environment RBAC setup
# This script helps diagnose common permission issues before deploying
# Run this locally with: bash docs/validate-staging-rbac.sh

set -e

echo "======================================================================"
echo "Staging Environment RBAC Validation"
echo "======================================================================"
echo ""

# Color output helpers
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

print_success() {
    echo -e "${GREEN}✓${NC} $1"
}

print_error() {
    echo -e "${RED}✗${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}⚠${NC} $1"
}

# Check if Azure CLI is installed
if ! command -v az &> /dev/null; then
    print_error "Azure CLI is not installed. Please install it from: https://learn.microsoft.com/cli/azure/install-azure-cli"
    exit 1
fi
print_success "Azure CLI is installed"

# Check if logged in
if ! az account show &> /dev/null; then
    print_error "Not logged in to Azure. Please run: az login"
    exit 1
fi
print_success "Logged in to Azure"

echo ""
echo "======================================================================"
echo "Step 1: Gather Configuration"
echo "======================================================================"
echo ""

# Prompt for required values
read -p "Enter your OIDC Client ID (AZURE_CLIENT_ID secret): " CLIENT_ID
if [ -z "$CLIENT_ID" ]; then
    print_error "Client ID is required"
    exit 1
fi

read -p "Enter your Azure Subscription ID (AZURE_SUBSCRIPTION_ID secret): " SUBSCRIPTION_ID
if [ -z "$SUBSCRIPTION_ID" ]; then
    print_error "Subscription ID is required"
    exit 1
fi

# Set defaults
RG_NAME="rg-systempromptbot-staging"
LOCATION="japaneast"

echo ""
echo "Configuration:"
echo "  OIDC Client ID: $CLIENT_ID"
echo "  Subscription ID: $SUBSCRIPTION_ID"
echo "  Resource Group: $RG_NAME"
echo "  Location: $LOCATION"
echo ""

echo "======================================================================"
echo "Step 2: Validate Service Principal"
echo "======================================================================"
echo ""

# Get service principal object ID
echo "Looking up service principal..."
SP_OBJECT_ID=$(az ad sp show --id "$CLIENT_ID" --query id --output tsv 2>/dev/null || echo "")

if [ -z "$SP_OBJECT_ID" ]; then
    print_error "Service principal not found for Client ID: $CLIENT_ID"
    print_error "Ensure the OIDC app registration exists and has a service principal in your tenant"
    exit 1
fi

SP_DISPLAY_NAME=$(az ad sp show --id "$CLIENT_ID" --query displayName --output tsv)
print_success "Found service principal: $SP_DISPLAY_NAME (Object ID: $SP_OBJECT_ID)"

echo ""
echo "======================================================================"
echo "Step 3: Check RBAC Role Assignments"
echo "======================================================================"
echo ""

echo "Checking role assignments for service principal..."
ROLE_ASSIGNMENTS=$(az role assignment list --assignee "$SP_OBJECT_ID" --all --output json)

# Check for Contributor or Owner role at subscription level
SUB_CONTRIBUTOR=$(echo "$ROLE_ASSIGNMENTS" | jq -r --arg sub "/subscriptions/$SUBSCRIPTION_ID" '.[] | select(.roleDefinitionName=="Contributor" or .roleDefinitionName=="Owner") | select(.scope==$sub) | .roleDefinitionName' | head -n 1)

if [ -n "$SUB_CONTRIBUTOR" ]; then
    print_success "Service principal has '$SUB_CONTRIBUTOR' role at subscription level"
    echo "  Scope: /subscriptions/$SUBSCRIPTION_ID"
    RBAC_OK=true
else
    print_warning "No Contributor or Owner role found at subscription level"
    
    # Check for resource group level
    RG_CONTRIBUTOR=$(echo "$ROLE_ASSIGNMENTS" | jq -r --arg rg "/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RG_NAME" '.[] | select(.roleDefinitionName=="Contributor" or .roleDefinitionName=="Owner") | select(.scope==$rg) | .roleDefinitionName' | head -n 1)
    
    if [ -n "$RG_CONTRIBUTOR" ]; then
        print_success "Service principal has '$RG_CONTRIBUTOR' role at resource group level"
        echo "  Scope: /subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RG_NAME"
        RBAC_OK=true
    else
        print_error "No Contributor or Owner role found at resource group level either"
        RBAC_OK=false
    fi
fi

echo ""
echo "All role assignments for this service principal:"
az role assignment list --assignee "$SP_OBJECT_ID" --all --query "[].{Role:roleDefinitionName,Scope:scope}" --output table

if [ "$RBAC_OK" != "true" ]; then
    echo ""
    print_error "RBAC VALIDATION FAILED"
    echo ""
    echo "The service principal lacks necessary permissions. Please assign Contributor role:"
    echo ""
    echo "  # Option A: Subscription-level (recommended)"
    echo "  az role assignment create \\"
    echo "    --role Contributor \\"
    echo "    --assignee-object-id $SP_OBJECT_ID \\"
    echo "    --assignee-principal-type ServicePrincipal \\"
    echo "    --scope /subscriptions/$SUBSCRIPTION_ID"
    echo ""
    echo "  # Option B: Resource group-level (more restrictive)"
    echo "  az group create --name $RG_NAME --location $LOCATION"
    echo "  az role assignment create \\"
    echo "    --role Contributor \\"
    echo "    --assignee-object-id $SP_OBJECT_ID \\"
    echo "    --assignee-principal-type ServicePrincipal \\"
    echo "    --scope /subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RG_NAME"
    echo ""
    exit 1
fi

echo ""
echo "======================================================================"
echo "Step 4: Check Resource Group"
echo "======================================================================"
echo ""

# Set active subscription
az account set --subscription "$SUBSCRIPTION_ID"
print_success "Set active subscription to: $SUBSCRIPTION_ID"

# Check if resource group exists
if az group show --name "$RG_NAME" --output none 2>/dev/null; then
    print_success "Resource group '$RG_NAME' exists"
    RG_LOCATION=$(az group show --name "$RG_NAME" --query location --output tsv)
    echo "  Location: $RG_LOCATION"
    
    if [ "$RG_LOCATION" != "$LOCATION" ]; then
        print_warning "Resource group location ($RG_LOCATION) differs from expected location ($LOCATION)"
    fi
else
    print_warning "Resource group '$RG_NAME' does not exist yet"
    echo "  It will be created automatically during first provision"
fi

echo ""
echo "======================================================================"
echo "Step 5: Check Federated Credentials"
echo "======================================================================"
echo ""

echo "Checking federated credentials for OIDC authentication..."
FED_CREDS=$(az ad app federated-credential list --id "$CLIENT_ID" --output json 2>/dev/null || echo "[]")

# Look for staging environment credential
STAGING_CRED=$(echo "$FED_CREDS" | jq -r '.[] | select(.subject | contains("environment:Staging")) | .name' | head -n 1)

if [ -n "$STAGING_CRED" ]; then
    print_success "Found federated credential for Staging environment: $STAGING_CRED"
    SUBJECT=$(echo "$FED_CREDS" | jq -r --arg name "$STAGING_CRED" '.[] | select(.name==$name) | .subject')
    echo "  Subject: $SUBJECT"
else
    print_error "No federated credential found for Staging environment"
    echo ""
    echo "You need to add a federated credential with subject containing 'environment:Staging'"
    echo "See docs/staging-setup.md Step 2 for instructions"
    echo ""
    exit 1
fi

echo ""
echo "======================================================================"
echo "Validation Summary"
echo "======================================================================"
echo ""

print_success "All validations passed!"
echo ""
echo "Your staging environment is configured correctly:"
echo "  ✓ Service principal exists and has correct role"
echo "  ✓ Federated credential is configured for Staging environment"
echo "  ✓ Azure CLI can access the subscription"
echo ""
echo "You should now be able to run the staging deployment workflow."
echo ""
echo "Next steps:"
echo "  1. Ensure GitHub secrets are set (see docs/staging-setup.md Step 4)"
echo "  2. Trigger the workflow: Actions → 'Staging - Deploy with azd' → Run workflow"
echo ""
