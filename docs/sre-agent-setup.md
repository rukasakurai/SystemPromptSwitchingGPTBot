# Azure SRE Agent Setup

This document describes how to set up the Azure SRE Agent for automated issue detection, troubleshooting, and remediation.

## What is Azure SRE Agent?

Azure SRE Agent is an AI-powered reliability assistant that:
- Monitors Azure resources 24/7 for anomalies and errors
- Automatically analyzes logs, metrics, and traces from Application Insights
- Provides natural language explanations of issues and root causes
- Suggests and can automate common remediation actions (with approval)
- Reduces Mean Time to Resolution (MTTR) and operational toil

## Prerequisites

This repository already has the required observability infrastructure:
- ✅ Application Insights configured in `infra/platform.bicep`
- ✅ Log Analytics workspace connected to App Insights
- ✅ Web App logging enabled in `infra/app.bicep`
- ✅ Managed Identity configured for the Web App

## Deployment Method

**Current Status (May 2026)**: Azure SRE Agent reached **General Availability in March 2026**. A stable ARM API version (`2026-01-01`) is now published for `Microsoft.App/agents`.

- **Recommended**: Azure Portal deployment (most fully documented end-to-end flow)
- **Supported**: Bicep deployment using the stable `2026-01-01` API

### Option 1: Azure Portal Deployment (Recommended)

1. **Navigate to Azure Portal**
   - Go to "Create a resource" → search "Azure SRE Agent" → Create

2. **Configure Agent**
   - **Subscription**: Your Azure subscription
   - **Resource Group**: Create new or use existing (e.g., `rg-sre-agents`)
   - **Agent Name**: `sre-{environmentName}` (e.g., `sre-production`)
   - **Region**: `East US 2`, `Sweden Central`, `Australia East`, or `UK South` (limited availability)

3. **Select Resources to Monitor**
   - Click "Choose resource groups"
   - Select the resource group containing your Web App and Application Insights
   - The agent will monitor all resources in selected groups

4. **Set Permission Level**
   - **Reader** (recommended for initial setup): Read-only access, prompts for approval on actions
   - **Privileged**: Can perform approved actions automatically (requires elevation)
   - Grants roles: `Reader`, `Monitoring Reader`, `Log Analytics Reader`

5. **Network Configuration**
   - Ensure firewall allows outbound traffic to `*.azuresre.ai`

6. **Create and Wait**
   - Deployment takes 2-5 minutes
   - Managed Identity is automatically created and assigned to monitored resource groups

### Option 2: Bicep Deployment

Bicep templates (`infra/sre-agent*.bicep`) deploy the agent using the stable `Microsoft.App/agents@2026-01-01` API.

**⚠️ Limitations**:
- The `properties` schema currently shipped in this repo is minimal — verify against the [API reference](https://learn.microsoft.com/en-us/azure/templates/microsoft.app/agents) for fields your scenario requires
- Child resources (data connectors, GitHub connector) are configured post-deployment via the [sre.azure.com](https://sre.azure.com) portal

**Deployment**:
```bash
# Step 1: Create resource group for the SRE Agent (if not exists)
az group create --name rg-sre-agents --location eastus2

# Step 2: Deploy SRE Agent at subscription scope (when ready for production use)
az deployment sub create \
  --location eastus2 \
  --template-file infra/sre-agent.bicep \
  --parameters \
    resourceGroupName=rg-sre-agents \
    agentName=sre-production \
    location=eastus2 \
    monitoredResourceGroupNames='["rg-app-production","rg-data-production"]' \
    permissionLevel=Reader
```

**Notes**: 
- Deployment is at subscription scope to allow RBAC assignments across multiple resource groups
- The resource group for the agent must exist before deployment
- The SRE Agent Bicep is **decoupled** from the main `azd` deployment (`infra/main.bicep` does not reference it) so it can be provisioned and deprovisioned independently of the application

### Deprovision and Reprovision On-Demand

The SRE Agent has a high always-on cost (~$288/month per agent). Because it lives in its own resource group and is not part of `azd up`, you can tear it down between experiments and bring it back when needed.

**Deprovision**:
```bash
az group delete --name rg-sre-agents --yes --no-wait
```

Role assignments left behind in monitored RGs are harmless and will be replaced cleanly on reprovision.

**Reprovision** — re-run the two-step deployment above, then reconfigure connectors and monitored scope at [sre.azure.com](https://sre.azure.com).

## Using the SRE Agent

### Chat Interface

1. Open Azure Portal → Navigate to your SRE Agent resource
2. Click "Chat" or "Investigate"
3. Ask questions in natural language:
   - "Why did error rates spike at 3 AM today?"
   - "Show me CancellationToken exceptions in the last 24 hours"
   - "Analyze the bot middleware failure from the screenshot"
   - "What's causing the async task timeout?"

### Automated Monitoring

The agent continuously:
- Scans Application Insights traces and exceptions
- Correlates errors across deployments
- Detects patterns (e.g., CancellationToken spikes after deployment)
- Alerts on anomalies via configured channels

### Remediation Actions

For common issues, the agent can:
- Restart App Service (if granted permissions)
- Scale resources (with approval)
- Rollback deployments (requires DevOps integration)
- Update configuration settings (with explicit approval)

**All actions require approval** unless you've configured automation rules.

## Integration with GitHub Issues

To let the SRE Agent read and act on this repository (analyze source, create issues, comment on PRs, trigger workflows), add the **GitHub connector**:

1. Open the agent at [sre.azure.com](https://sre.azure.com) → **Builder** → **Knowledge base** → **Add repository**
2. Choose **GitHub** and authenticate (OAuth recommended; PAT requires `repo` scope)
3. Select this repository (or paste its URL)

The agent can then create / update / comment on issues and PRs and trigger GitHub Actions workflows as part of its investigation and remediation flow. See [GitHub connector in Azure SRE Agent](https://learn.microsoft.com/en-us/azure/sre-agent/github-connector) for details.

## Permissions and RBAC

### Agent Managed Identity

The SRE Agent uses its system-assigned Managed Identity with these roles on monitored resource groups:

- `Reader` - Read resource properties and status
- `Monitoring Reader` - Read metrics and logs
- `Log Analytics Reader` - Query Log Analytics workspace

### Optional Privileged Actions

For automatic remediation, add:
- `Website Contributor` - Restart Web Apps
- `Contributor` - Full resource management (use cautiously)

**Best Practice**: Start with `Reader`, grant elevated permissions only after testing.

## Observability Integration

The SRE Agent automatically connects to:

- **Application Insights**: `appi-{resourceToken}` (from `infra/platform.bicep`)
- **Log Analytics**: `log-{resourceToken}` (from `infra/platform.bicep`)
- **Web App Logs**: File system logs enabled in `infra/app.bicep`

No additional configuration needed - the agent discovers these through resource group association.

## Troubleshooting

### Agent Not Detecting Issues

1. **Verify Resource Group Assignment**: Ensure monitored resource groups are correct
2. **Check Application Insights Data**: Confirm telemetry is flowing (Portal → App Insights → Logs)
3. **Wait for Initial Indexing**: New agents need 10-15 minutes to index existing data

### Permission Errors

- **"Agent cannot read resource"**: Add `Reader` role to agent's managed identity
- **"Cannot execute action"**: Requires elevated permissions or user approval via OBO flow

### Region Limitations

If deployment fails, verify region support:
- **Supported (as of May 2026)**: East US 2, Sweden Central, Australia East, UK South
- **Workaround**: Deploy agent in a supported region; it can monitor resources in any region

## Cost Considerations

Azure SRE Agent is billed in **Azure Agent Units (AAUs)**:

- **Always-on flow (fixed)**: 4 AAUs/hour per agent, billed continuously (~$288/month per agent before any usage)
- **Active flow (usage-based)**: additional AAUs per million tokens processed during investigations and actions; rate varies by model

See [Pricing and billing for Azure SRE Agent](https://learn.microsoft.com/en-us/azure/sre-agent/pricing-billing).

To minimize cost:
- Limit monitored resource groups to critical services only
- Set a monthly AAU cap
- Deprovision the agent when not in use (see [Deprovision and Reprovision On-Demand](#deprovision-and-reprovision-on-demand))

## References

- [Azure SRE Agent Documentation](https://learn.microsoft.com/en-us/azure/sre-agent/)
- [Create and Use an Agent](https://learn.microsoft.com/en-us/azure/sre-agent/usage)
- [Agent Permissions](https://learn.microsoft.com/en-us/azure/sre-agent/permissions)
- [User Roles and Permissions](https://learn.microsoft.com/en-us/azure/sre-agent/user-roles)
- [Supported Regions](https://learn.microsoft.com/en-us/azure/sre-agent/supported-regions)
- [Pricing and Billing](https://learn.microsoft.com/en-us/azure/sre-agent/pricing-billing)
- [Microsoft.App/agents ARM/Bicep reference](https://learn.microsoft.com/en-us/azure/templates/microsoft.app/agents)
- [Product portal](https://sre.azure.com)

## Next Steps

1. Deploy SRE Agent via Azure Portal (recommended)
2. Configure monitoring for resource group containing Web App
3. Test chat interface with recent error scenarios
4. Set up GitHub integration for automated issue creation
5. Refine automation rules based on operational patterns
6. Monitor agent effectiveness and MTTR improvements
