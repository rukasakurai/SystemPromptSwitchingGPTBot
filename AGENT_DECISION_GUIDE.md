# Microsoft Entra ID Agent Decision Guide

This guide helps you choose the appropriate identity pattern for your Azure and Microsoft 365 integration scenarios. It provides clear decision criteria based on security, compliance, multi-tenancy, and operational requirements.

## Overview: Identity Patterns in Azure

When building applications that interact with Azure services, Microsoft Graph API, or Microsoft 365, you must choose the right identity pattern. This decision impacts security, scalability, compliance, and operational complexity.

### Key Identity Patterns

1. **Managed Identity** - Azure-native, passwordless identity for Azure resources
2. **App Registration (Service Principal)** - Application identity with credentials
3. **User Identity** - Human user accounts in Entra ID
4. **Federated Identity** - OIDC-based workload identity (e.g., GitHub Actions)

## Decision Framework

### When to Use Managed Identity (Preferred)

**Use Managed Identity when:**
- ✅ Your application runs on Azure compute (App Service, Functions, VMs, AKS, Container Apps)
- ✅ You need to access Azure services (Storage, Key Vault, SQL, OpenAI, etc.)
- ✅ You want passwordless, automatic credential rotation
- ✅ You want to minimize security risk (no secrets to manage)
- ✅ Your scenario is single-tenant within one Azure subscription

**Types:**
- **System-Assigned**: Lifecycle tied to the resource; use for most scenarios
- **User-Assigned**: Shared across resources; use when multiple resources need the same identity

**Example Use Cases:**
- Web App accessing Azure OpenAI
- Function App reading from Key Vault
- Container App writing to Cosmos DB
- VM accessing Azure Storage

**Implementation:**
```csharp
// Use DefaultAzureCredential - automatically uses Managed Identity in Azure
var credential = new DefaultAzureCredential();
var client = new OpenAIClient(new Uri(endpoint), credential);
```

**RBAC Assignment:**
```bash
# Assign role to the Managed Identity (Service Principal)
az role assignment create \
  --assignee <managed-identity-principal-id> \
  --role "Cognitive Services OpenAI User" \
  --scope <resource-id>
```

### When to Use App Registration with Client Secret

**Use App Registration when:**
- ✅ You need to authenticate from non-Azure environments (on-premises, other clouds)
- ✅ Bot Framework requires it (Bot ↔ Bot Service authentication)
- ✅ You need to access Microsoft Graph API with application permissions
- ✅ You require multi-tenant application support
- ✅ Your application needs delegated permissions on behalf of users

**Critical for:**
- **Bot Framework Identity**: Required for Bot Service authentication
  - `MicrosoftAppId`: The App Registration's Application (client) ID
  - `MicrosoftAppPassword`: Client secret value
  - `MicrosoftAppTenantId`: Your Entra ID tenant ID
  - `MicrosoftAppType`: `SingleTenant` (recommended) or `MultiTenant`

**Security Requirements:**
- 🔐 Store secrets in Azure Key Vault (never in code or appsettings.json)
- 🔐 Implement secret rotation (secrets expire, typically 90 days to 2 years)
- 🔐 Use least-privilege API permissions
- 🔐 Consider federated credentials (OIDC) instead of secrets when possible

**Example Use Cases:**
- Bot Framework bot authentication
- Daemon application accessing Microsoft Graph
- Multi-tenant SaaS application
- GitHub Actions deploying to Azure (use federated identity instead if possible)

### When to Use Federated Identity (Workload Identity)

**Use Federated Identity when:**
- ✅ You need passwordless authentication for CI/CD pipelines
- ✅ GitHub Actions, Azure DevOps, or other OIDC providers
- ✅ You want to eliminate long-lived secrets in pipelines
- ✅ You need secure, auditable automation

**Benefits:**
- No secrets to store or rotate
- Short-lived tokens (typically 1 hour)
- Better compliance and audit trail
- Works with GitHub Actions, GitLab, Azure DevOps

**Implementation:**
```yaml
# GitHub Actions with federated identity
- uses: azure/login@v1
  with:
    client-id: ${{ secrets.AZURE_CLIENT_ID }}
    tenant-id: ${{ secrets.AZURE_TENANT_ID }}
    subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
```

**Setup via Azure CLI:**
```bash
# Create App Registration
az ad app create --display-name "MyApp" --sign-in-audience "AzureADMyOrg"

# Create federated credential for GitHub Actions
az ad app federated-credential create \
  --id <app-object-id> \
  --parameters '{
    "name": "github-actions-federated-credential",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:org/repo:environment:prod",
    "audiences": ["api://AzureADTokenExchange"]
  }'
```

**Note:** App Registrations and federated credentials cannot be managed directly in Bicep. Use Azure CLI, PowerShell, or Terraform with the AzureAD provider.

### When to Use User Identity

**Use User Identity when:**
- ✅ Local development and testing (via `az login`)
- ✅ Interactive applications requiring user consent
- ✅ Delegated permissions scenarios (act on behalf of user)

**DO NOT use User Identity for:**
- ❌ Production services or daemons
- ❌ Non-interactive workloads
- ❌ CI/CD pipelines

**Local Development Pattern:**
```bash
# Developer logs in with their user account
az login

# Application uses DefaultAzureCredential (falls back to az login)
# Developer needs RBAC on the resource (e.g., "Cognitive Services OpenAI User")
```

## Microsoft Graph API Scenarios

### Application Permissions vs Delegated Permissions

**Application Permissions** (App-only):
- Application acts with its own identity
- No user context required
- Requires admin consent
- Use for: background services, daemons, automation
- Example: App reads all users in the directory

**Delegated Permissions** (On-behalf-of):
- Application acts on behalf of a signed-in user
- User context preserved
- May require user or admin consent
- Use for: interactive apps, user-specific data access
- Example: App reads the signed-in user's calendar

### Common Graph API Patterns

**Pattern 1: Background Service Reading Organization Data**
```
Identity: App Registration with Application Permissions
Permissions: User.Read.All, Group.Read.All (with admin consent)
Auth Flow: Client Credentials
```

**Pattern 2: Bot Accessing User Profile**
```
Identity: Bot Framework App Registration
Permissions: User.Read (delegated, basic profile)
Auth Flow: Bot Framework token exchange
```

**Pattern 3: Interactive App Accessing User Data**
```
Identity: App Registration with Delegated Permissions
Permissions: Calendars.Read, Mail.Read (user consent)
Auth Flow: Authorization Code with PKCE
```

## Multi-Tenancy Considerations

### Single-Tenant vs Multi-Tenant

**Single-Tenant** (`signInAudience: AzureADMyOrg`):
- App Registration restricted to your tenant only
- Users from other tenants cannot sign in
- Simpler security model
- **Recommended for most scenarios**
- Use for: internal apps, bots for one organization

**Multi-Tenant** (`signInAudience: AzureADMultipleOrgs`):
- App Registration can authenticate users from any Entra ID tenant
- Requires tenant-specific consent and validation
- Complex security and compliance requirements
- Use for: ISV applications, SaaS products

**Key Multi-Tenant Concerns:**
1. **Tenant Isolation**: Ensure data from one tenant doesn't leak to another
2. **Consent Management**: Handle admin consent per tenant
3. **Service Principal Provisioning**: Each tenant gets its own Service Principal
4. **Conditional Access**: Each tenant may have different CA policies
5. **Compliance**: Different tenants may have different compliance requirements

### Multi-Tenant Architecture Pattern

```
Customer Tenant A → Service Principal A ─┐
Customer Tenant B → Service Principal B ─┤→ App Registration (Multi-Tenant) → Your Service
Customer Tenant C → Service Principal C ─┘
```

**Implementation Considerations:**
- Use `tid` (tenant ID) claim to identify which tenant the user belongs to
- Store tenant-specific configuration separately
- Implement tenant-level feature flags and quotas
- Consider separate data stores per tenant for compliance

## Security and Compliance Best Practices

### Principle of Least Privilege

**Always assign the minimum necessary permissions:**
```
✅ Good: "Cognitive Services OpenAI User" (read/write data only)
❌ Bad: "Contributor" (can modify resource configuration)
❌ Bad: "Owner" (full control including RBAC changes)
```

### Credential Management

**Managed Identity**: No credentials to manage ✅

**Client Secrets**:
- Store in Azure Key Vault
- Rotate before expiration
- Use short expiration periods (90 days recommended)
- Monitor expiration via Azure Monitor

**Federated Credentials**:
- No long-lived secrets ✅
- Tokens expire in 1 hour
- Tightly scoped to specific repos/environments

### Audit and Compliance

**Enable Entra ID Sign-in Logs:**
- Track all authentication attempts
- Monitor for suspicious activity
- Integrate with Azure Sentinel for advanced detection

**Enable Entra ID Audit Logs:**
- Track changes to App Registrations
- Monitor RBAC assignments
- Review API permission grants

**Resource-Level Diagnostics:**
- Enable diagnostic logs on Azure resources
- Send to Log Analytics workspace
- Create alerts for unauthorized access attempts

## Common Mistakes and How to Avoid Them

### ❌ Mistake 1: Assigning RBAC to App Registration

**Wrong:**
```bash
# This doesn't work - App Registration is not a security principal in Azure RBAC
az role assignment create --assignee <app-registration-id> --role "Contributor"
```

**Correct:**
```bash
# Assign to the Service Principal (Enterprise Application)
az role assignment create --assignee <service-principal-id> --role "Contributor"

# Or use Managed Identity principal ID
az role assignment create --assignee <managed-identity-principal-id> --role "Contributor"
```

**Why:** App Registrations are identity definitions. Service Principals are the runtime security principals in a tenant.

### ❌ Mistake 2: Storing Secrets in Code or Config Files

**Wrong:**
```json
// appsettings.json - NEVER DO THIS
{
  "MicrosoftAppPassword": "abc123secretvalue"
}
```

**Correct:**
```json
// appsettings.json - Reference Key Vault
{
  "MicrosoftAppPassword": "@Microsoft.KeyVault(SecretUri=https://myvault.vault.azure.net/secrets/AppPassword/)"
}
```

**Better:** Use Managed Identity and eliminate the secret entirely (when possible).

### ❌ Mistake 3: Using User Accounts for Services

**Wrong:**
```
Service uses a human user account (john.doe@company.com) to authenticate
```

**Problems:**
- Account may be disabled when employee leaves
- Password expiration causes service outages
- Violates compliance policies
- Poor audit trail

**Correct:** Use Managed Identity or Service Principal.

### ❌ Mistake 4: Over-Privileged Permissions

**Wrong:**
```
Granting "Contributor" or "Owner" when only data access is needed
Granting "User.ReadWrite.All" when only "User.Read.All" is needed
```

**Correct:**
- Use data-plane roles (e.g., "Cognitive Services OpenAI User")
- Request minimum Graph API permissions
- Review permissions quarterly

### ❌ Mistake 5: Ignoring Secret Expiration

**Problem:** Client secrets expire, causing production outages.

**Solution:**
1. Set calendar reminders for secret rotation
2. Use Azure Monitor to alert on expiring secrets
3. Implement automated secret rotation
4. Consider federated credentials (no expiration)

## Decision Tree

```
START: Need to authenticate an application to Azure/M365?
│
├─ Running on Azure compute (App Service, Functions, AKS, VM)?
│  └─ YES → Use Managed Identity (System-Assigned)
│     └─ Assign RBAC to the Managed Identity's Service Principal
│
├─ CI/CD pipeline (GitHub Actions, Azure DevOps)?
│  └─ YES → Use Federated Identity (Workload Identity)
│     └─ Configure OIDC trust between provider and App Registration
│
├─ Bot Framework bot?
│  └─ YES → Use App Registration with Client Secret
│     └─ Type: SingleTenant (most cases)
│     └─ Store secret in Key Vault
│
├─ Need to access Microsoft Graph with app permissions?
│  └─ YES → Use App Registration with Application Permissions
│     └─ Get admin consent
│     └─ Use client secret or certificate (prefer certificate)
│
├─ Local development only?
│  └─ YES → Use User Identity (az login)
│     └─ Assign RBAC to your user account on Azure resources
│
└─ Multi-tenant SaaS application?
   └─ YES → Use Multi-Tenant App Registration
      └─ Implement per-tenant Service Principal handling
      └─ Consider certificate-based auth
```

## Implementation Checklist

### For Managed Identity Pattern

- [ ] Enable system-assigned Managed Identity on Azure resource
- [ ] Assign RBAC role to Managed Identity's Service Principal
- [ ] Use `DefaultAzureCredential` in application code
- [ ] Test locally with `az login` (user identity)
- [ ] Verify role assignment in Azure Portal (IAM blade)

### For App Registration Pattern

- [ ] Create App Registration in Entra ID
- [ ] Configure app type (SingleTenant or MultiTenant)
- [ ] Generate client secret (or use certificate)
- [ ] Store secret in Azure Key Vault
- [ ] Grant API permissions (if using Microsoft Graph)
- [ ] Get admin consent for application permissions
- [ ] Assign RBAC to the Service Principal (not App Registration)
- [ ] Set up secret expiration monitoring
- [ ] Document secret rotation procedure

### For Federated Identity Pattern

- [ ] Create App Registration in Entra ID
- [ ] Configure federated credential (OIDC trust)
- [ ] Set issuer, subject, and audience correctly
- [ ] Assign RBAC to the Service Principal
- [ ] Update CI/CD pipeline to use OIDC auth
- [ ] Remove any stored client secrets
- [ ] Test pipeline deployment

## Additional Resources

### Microsoft Documentation
- [Managed Identities for Azure Resources](https://learn.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/)
- [Application and Service Principal Objects](https://learn.microsoft.com/en-us/azure/active-directory/develop/app-objects-and-service-principals)
- [Microsoft Graph Permissions Reference](https://learn.microsoft.com/en-us/graph/permissions-reference)
- [Workload Identity Federation](https://learn.microsoft.com/en-us/azure/active-directory/develop/workload-identity-federation)

### Repository-Specific Guidance
- See `AGENTS.md` Section 3 for Azure/Entra Identity Guidelines
- See `.github/agents/azure-identity-access.md` for specialist agent guidance
- See `infra/role.bicep` for RBAC assignment examples
- See `docs/azure-oidc-setup.md` for federated identity setup

## Frequently Asked Questions

### Q: When should I use user-assigned vs system-assigned Managed Identity?

**A:** Use **system-assigned** (default recommendation) when:
- Identity is tied to a single resource's lifecycle
- Simple scenarios with one resource accessing other Azure services

Use **user-assigned** when:
- Multiple resources need the same identity
- Identity should persist independently of resources
- Complex cross-subscription scenarios

### Q: Can I use Managed Identity to access Microsoft Graph API?

**A:** Yes, but with limitations:
- Managed Identities support application permissions (app-only)
- You assign Graph API permissions to the Service Principal
- Use for background services accessing org data
- Cannot use for delegated (user context) scenarios

### Q: How do I rotate Bot Framework client secrets without downtime?

**A:** Follow this process:
1. Create a second client secret (App Registration supports multiple)
2. Update Azure Web App configuration with new secret
3. Verify bot is working with new secret
4. Delete old secret from App Registration
5. Set reminder to rotate again before expiration

### Q: What's the difference between Enterprise Application and App Registration?

**A:**
- **App Registration**: Identity definition (metadata), created in Entra ID
- **Enterprise Application**: Service Principal (runtime security principal)
- When you create an App Registration, an Enterprise Application is automatically created
- Assign RBAC to the Enterprise Application (Service Principal), not the App Registration

### Q: Should I use multi-tenant for my bot?

**A:** Use **SingleTenant** unless you have a specific need:
- ✅ SingleTenant: Bot for one organization (recommended)
- ⚠️ MultiTenant: Bot as a service for multiple organizations (complex)

Multi-tenant requires:
- Per-tenant consent management
- Tenant isolation in your code/data
- Complex security validation
- Compliance considerations

---

## Document Maintenance

This guide should be reviewed and updated when:
- New Entra ID/Azure identity features are released
- Security best practices evolve
- New authentication patterns emerge in the repository
- Feedback indicates confusion or gaps in guidance

Last reviewed: 2025-12-16
