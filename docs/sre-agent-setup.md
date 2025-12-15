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

**Current Status (December 2024)**: Azure SRE Agent is in **preview** with limited IaC support.

- **Recommended**: Azure Portal deployment (stable, fully documented)
- **Experimental**: Bicep deployment (preview API, schema incomplete)

### Option 1: Azure Portal Deployment (Recommended)

1. **Navigate to Azure Portal**
   - Go to "Create a resource" → search "Azure SRE Agent" → Create

2. **Configure Agent**
   - **Subscription**: Your Azure subscription
   - **Resource Group**: Create new or use existing (e.g., `rg-sre-agents`)
   - **Agent Name**: `sre-{environmentName}` (e.g., `sre-production`)
   - **Region**: `East US 2` or `Sweden Central` (limited availability)

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

### Option 2: Bicep Deployment (Experimental)

Bicep templates (`infra/sre-agent*.bicep`) are provided as a **foundation** for when the API becomes stable.

**⚠️ Limitations**:
- API version `2025-05-01-preview` is subject to breaking changes
- Full schema is not publicly documented
- Child resources (data connectors) may require manual configuration

**Deployment**:
```bash
# Deploy SRE Agent at subscription scope (when ready for production use)
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

**Note**: Deployment is at subscription scope to allow RBAC assignments across multiple resource groups.

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

To connect the SRE Agent with GitHub for automated issue creation:

1. **Create GitHub App** (one-time setup):
   - Settings → Developer settings → GitHub Apps → New
   - Grant permissions: Issues (read/write), Repository contents (read)
   - Generate and save private key

2. **Configure SRE Agent Integration**:
   - In Azure Portal → SRE Agent → Settings → Integrations
   - Add GitHub integration
   - Provide GitHub App ID and private key (store in Key Vault)
   - Select repositories to create issues in

3. **Automation Rules**:
   - Configure thresholds: "Create issue if error rate > 50/min for 5 min"
   - Define issue templates with agent-generated diagnostics

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
- **Supported**: East US 2, Sweden Central, Australia East (as of Dec 2024)
- **Workaround**: Deploy agent in supported region; it can monitor resources in any region

## Cost Considerations

Azure SRE Agent billing:
- **Agent Units (AAUs)**: Pay-as-you-go based on monitored resources and query volume
- **Approximate**: $50-200/month for small-medium deployments
- **Free Tier**: Not available during preview

To minimize costs:
- Limit monitored resource groups to critical services only
- Configure alert thresholds to reduce noise
- Review agent usage metrics monthly

## References

- [Azure SRE Agent Documentation](https://learn.microsoft.com/en-us/azure/sre-agent/)
- [Create and Use an Agent](https://learn.microsoft.com/en-us/azure/sre-agent/usage)
- [Agent Permissions](https://learn.microsoft.com/en-us/azure/sre-agent/agent-managed-identity)
- [FAQ](https://learn.microsoft.com/en-us/azure/sre-agent/faq)

## Next Steps

1. Deploy SRE Agent via Azure Portal (recommended for preview)
2. Configure monitoring for resource group containing Web App
3. Test chat interface with recent error scenarios
4. Set up GitHub integration for automated issue creation
5. Refine automation rules based on operational patterns
6. Monitor agent effectiveness and MTTR improvements
