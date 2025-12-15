# AI Agent Collaboration Contract

This document establishes the contract between this repository and AI agents. It externalizes assumptions that materially affect correctness, security, cost, and debuggability.

## 1. Canonical Execution Environment

### Reference Platforms
- **Supported OS**: Windows and Linux (both fully supported for CI and local development)
  - .NET and Azure Web Apps have first-class support on both platforms
- **Shells**: PowerShell/pwsh (Windows), bash (Linux) - commands work cross-platform
- **.NET Version**: 8.0 (with forward compatibility to .NET 10 for tests)

### CI Parity
- Local development MUST match CI environment capabilities
- CI workflows are the source of truth for validation:
  - `.github/workflows/pr-tests.yml` - .NET test validation
  - `.github/workflows/bicep-validation.yml` - Infrastructure validation
  - `.github/workflows/teams-app-ci.yml` - Teams app packaging

### Dev Container Policy
- **Not currently used** - agents should not assume dev containers
- Development can occur natively with .NET 8 SDK installed
- Azure CLI required for local Azure resource interaction

## 2. Repeatable Setup Invariants

### CLI/Code-First Principle
- **Prefer scriptable setup steps** via CLI commands or code where possible
- Configuration is declarative in:
  - `.csproj` files for dependencies
  - `appsettings.json` / `appsettings.Development.json` for app config
  - Bicep files (`infra/*.bicep`) for Azure resources
- Some operations (e.g., certain Entra ID or Teams configurations) may require Portal/GUI

### Declarative Infrastructure as Source of Truth
- **Bicep files in `infra/` are authoritative** for Azure resource definitions
- Prefer making infrastructure changes in Bicep when possible
- Main deployment entry point: `infra/main.bicep`
- Module structure:
  - `platform.bicep` - Reusable platform resources (App Service Plan, OpenAI, App Insights)
  - `app.bicep` - Bot-specific resources (Web App, Bot Service)
  - `role.bicep` - RBAC assignments
- **Bicep API versions**: Use latest stable (non-preview) API versions from [Azure Templates documentation](https://learn.microsoft.com/en-us/azure/templates). Avoid preview versions unless they provide essential functionality not available in stable versions.
  
### GUI/Portal Usage Policy
- Portal is useful for exploration, verification, and certain operations that lack CLI/IaC support
- Prefer documenting scriptable approaches when available
- For Portal-only operations (e.g., some Entra ID/Teams setup), document clearly

## 3. Azure / Entra Identity Guidelines

### App Registrations vs Service Principals
- **App Registrations define applications** - they are the identity definition
- **Service Principals are the security principals** - they are the runtime identity in a tenant
- When an App Registration is created, a Service Principal is automatically created in the tenant
- **Enterprise Applications** in Portal are the SP representation - use for viewing, NOT creating

### RBAC Assignment Rules
- **Assign RBAC to Service Principals** (the runtime identity), not App Registrations (which are just metadata)
- For Azure OpenAI: Assign "Cognitive Services OpenAI User" role to:
  - Web App's Managed Identity (system-assigned)
  - Developer's user identity (for local dev)
  
### Identity Types by Use Case
- **Bot Framework identity**: App Registration with client secret (for Bot ↔ Bot Service auth)
  - Config: `MicrosoftAppId`, `MicrosoftAppPassword`, `MicrosoftAppTenantId`
  - Type: `SingleTenant` (recommended for single-tenant scenarios)
- **Web App to Azure OpenAI**: Managed Identity (system-assigned, passwordless)
  - Web App's identity gets RBAC on OpenAI resource
  - Uses `DefaultAzureCredential` in code
- **Local dev to Azure OpenAI**: User identity via `az login`
  - Developer's user account gets RBAC on OpenAI resource

### Common Mistakes to AVOID
- ❌ Using user accounts for non-interactive workloads
- ❌ Creating multiple App Registrations when Managed Identity suffices
- ❌ Storing credentials in code or appsettings.json (prefer managed identity; use Key Vault if secrets are necessary)

## 4. Observability Model Invariants

### Logging vs Telemetry vs Visualization
- **Logging**: Console logs captured by App Service, viewable in Log Stream
- **Telemetry**: Automatic HTTP requests, dependencies, exceptions sent to Application Insights
- **Visualization**: Application Insights dashboards, KQL queries in Azure Monitor
- IaC (`infra/platform.bicep`) provisions Application Insights and configures Web App connection

### Required Logging Properties
- When adding structured logging, include:
  - `ConversationId` - for correlating messages in a conversation
  - `ActivityId` - Bot Framework activity identifier
  - `UserId` - for user-specific diagnostics (anonymized if necessary)
  - `Timestamp` - for sequence reconstruction

### IaC Reflection of Observability
- Application Insights resource should be in `infra/platform.bicep`
- Web App should reference App Insights connection string in `infra/app.bicep`
- Prefer provisioning observability tools via Bicep when possible

### Azure SRE Agent for Automated Troubleshooting
- **Purpose**: AI-powered reliability assistant for proactive issue detection and remediation
- **Status**: Preview service with limited IaC support (as of December 2024)
- **Deployment**: Currently recommended via Azure Portal (see `docs/sre-agent-setup.md`)
- **IaC Foundation**: `infra/sre-agent.bicep` provided as template for future GA deployment
- **Integration**: Automatically discovers Application Insights, Log Analytics, and Web App logs
- **Permissions**: Uses managed identity with Reader/Monitoring Reader/Log Analytics Reader roles
- **Best Practice**: Deploy after initial infrastructure is stable; useful for production monitoring

## 5. What AGENTS.md Cannot Solve (and Mitigations)

### Enforcement
- **Limitation**: Markdown states rules but cannot enforce them
- **Mitigations**:
  - CI checks (`.github/workflows/pr-tests.yml`, `bicep-validation.yml`)
  - Bicep parameter validation and allowed value constraints
  - PR review checklist (if introduced)

### Ground Truth of Platform Behavior
- **Limitation**: Documentation describes intent, not runtime reality
- **Mitigations**:
  - Bot Framework Emulator for local validation
  - Application Insights for production behavior verification
  - KQL queries to validate telemetry flow (e.g., `requests | where name contains "messages"`)

### Judgment and Prioritization
- **Limitation**: Tradeoffs require human decision-making
- **Mitigations**:
  - Agents escalate ambiguous decisions
  - Lightweight ADRs for architectural changes (if pattern emerges)
  - Explicit ownership: identity/infrastructure changes need human approval

## 6. Documentation Guidelines

- Documentation (e.g., README.md) should be **very concise**
- New documentation should be **rarely added** unless it prevents repeated human intervention
- **Correcting incorrect documentation is encouraged**
- **Agents are encouraged to suggest improvements to AGENTS.md** to keep it accurate and useful
- If adding docs, ensure they follow the CLI/code-first principle where possible

## Acceptance Checklist for Agents

Before completing a task, verify:
- [ ] Change builds successfully (`dotnet build`)
- [ ] All tests pass (`dotnet test`)
- [ ] Infrastructure validates (if Bicep changed)
- [ ] No hardcoded secrets or credentials introduced
- [ ] RBAC guidelines followed (if identity/auth changed)
- [ ] Observability intent preserved (if logging/telemetry changed)
- [ ] Work is scriptable via CLI where possible (document Portal steps if necessary)
