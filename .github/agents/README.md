# Domain Specialist Agents

This directory contains custom AI agent definitions for GitHub Copilot. These agents are specialized experts that can assist with specific technical domains in this repository.

## Available Specialists

### 1. Azure Infrastructure & Bicep Specialist
**File**: `azure-infrastructure-bicep.md`

Expert in Azure IaC (Bicep/ARM), resource provisioning, and infrastructure validation. Specializes in:
- Bicep template development and modularization
- Azure resource architecture (App Service, Bot Service, OpenAI, etc.)
- RBAC and security configuration
- Infrastructure validation and deployment

**Use When**: Working with files in `infra/` directory, Azure resource provisioning, Bicep validation

---

### 2. Azure Identity & Access Management Specialist
**File**: `azure-identity-access.md`

Expert in Azure AD/Entra ID, managed identities, RBAC, and authentication patterns. Specializes in:
- App Registrations and Service Principals
- Managed Identity configuration
- RBAC assignments and least-privilege access
- Bot Framework authentication
- Federated credentials and OIDC

**Use When**: Configuring authentication, managing identities, troubleshooting authorization issues

---

### 3. .NET Development & Migration Specialist
**File**: `dotnet-development.md`

Expert in .NET 8/10 development and Bot Framework SDK. Specializes in:
- .NET application development and architecture
- Bot Framework SDK integration
- Azure SDK for .NET usage
- Dependency injection and configuration
- .NET 8 to .NET 10 migration

**Use When**: Working with C# code in `app/` or `tests/`, adding features, fixing bugs, upgrading frameworks

---

### 4. Azure OpenAI & AI Services Specialist
**File**: `azure-openai-services.md`

Expert in Azure OpenAI Service and GPT model integration. Specializes in:
- Azure OpenAI configuration and deployment
- GPT model selection and parameter tuning
- System prompt engineering
- Chat completion API integration
- Token optimization and cost management

**Use When**: Working with OpenAI configurations, system prompts in `app/GptConfiguration/`, API integration

---

### 5. SRE & Observability Specialist
**File**: `sre-observability.md`

Expert in Application Insights, monitoring, and operational excellence. Specializes in:
- Application Insights and Azure Monitor
- Structured logging and telemetry
- KQL queries and dashboards
- Performance monitoring and diagnostics
- Incident troubleshooting

**Use When**: Debugging issues, adding logging, creating monitoring dashboards, analyzing performance

---

### 6. Documentation & Developer Experience Specialist
**File**: `documentation-devex.md`

Expert in technical documentation and developer onboarding. Specializes in:
- Clear, concise technical writing
- Setup guides and troubleshooting docs
- Repository structure and organization
- Multilingual documentation (Japanese/English)
- Developer experience optimization

**Use When**: Creating or updating documentation, improving setup guides, enhancing README files

---

### 7. Teams & Microsoft 365 Integration Specialist
**File**: `teams-integration.md`

Expert in Microsoft Teams app development and Bot Framework. Specializes in:
- Teams app manifest configuration
- Bot Framework activity handling
- Adaptive Cards and messaging
- Azure Bot Service setup
- Teams app deployment and distribution

**Use When**: Working with `manifest/` directory, Teams app configuration, bot behavior, app deployment

---

## How to Use These Agents

### In GitHub Copilot Chat
When working on a task related to a specific domain, you can invoke these specialists by asking Copilot to use the relevant agent. For example:

```
@copilot Using the Azure Infrastructure & Bicep Specialist agent, 
help me add a new resource to platform.bicep
```

### Agent Selection Guide
Choose the right specialist based on your task:

| Task Area | Recommended Agent |
|-----------|------------------|
| Infrastructure changes | Azure Infrastructure & Bicep |
| Authentication issues | Azure Identity & Access Management |
| Application code changes | .NET Development & Migration |
| OpenAI integration | Azure OpenAI & AI Services |
| Monitoring and debugging | SRE & Observability |
| Documentation updates | Documentation & Developer Experience |
| Teams app changes | Teams & Microsoft 365 Integration |

### Multi-Agent Collaboration
Some tasks may benefit from multiple specialists:
- **Deploying new Azure resources**: Infrastructure + Identity specialists
- **Adding new bot features**: .NET Development + Teams Integration specialists
- **Optimizing OpenAI usage**: Azure OpenAI + SRE specialists
- **Complete feature implementation**: Multiple specialists as needed

## Agent Design Philosophy

These agents follow the repository's principles:
- **Minimal changes**: Surgical, precise modifications
- **CLI/code-first**: Prefer scriptable approaches
- **Security-first**: Follow least-privilege and secure practices
- **Documentation**: Maintain concise, accurate documentation
- **Testing**: Ensure changes pass validation workflows

## Maintenance

These agent definitions should be updated when:
1. Repository structure or practices change significantly
2. New patterns or best practices are established
3. Azure services or .NET versions are upgraded
4. Common issues or patterns emerge from GitHub issues/PRs

For updates, ensure consistency with `AGENTS.md` and repository conventions.

## References

- [GitHub Copilot Custom Agents Documentation](https://docs.github.com/en/copilot/how-tos/use-copilot-agents/coding-agent/create-custom-agents)
- [Repository AGENTS.md](../../AGENTS.md) - AI agent collaboration contract
- [Azure MCP Server](https://learn.microsoft.com/en-us/azure/developer/azure-mcp-server/)
