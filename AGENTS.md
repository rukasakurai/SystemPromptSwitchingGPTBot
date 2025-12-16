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

## 7. Domain Specialist Agents

This repository includes **custom AI agents** in `.github/agents/` that provide specialized expertise:

### Available Specialists
1. **Azure Infrastructure & Bicep** (`azure-infrastructure-bicep.md`) - IaC, Bicep templates, resource provisioning
2. **Azure Identity & Access Management** (`azure-identity-access.md`) - Entra ID, managed identities, RBAC
3. **.NET Development & Migration** (`dotnet-development.md`) - .NET 8/10, Bot Framework SDK
4. **Azure AI Services** (`azure-ai-services.md`) - Azure AI, GPT integration, prompt engineering
5. **SRE & Observability** (`sre-observability.md`) - Application Insights, monitoring, KQL
6. **Documentation & Developer Experience** (`documentation-devex.md`) - Technical writing, developer onboarding
7. **Teams & Microsoft 365 Integration** (`teams-integration.md`) - Teams apps, Bot Framework

### Using Domain Specialists
- These agents are available through GitHub Copilot for domain-specific tasks
- Each agent aligns with the guidelines in this AGENTS.md document
- Prefer delegating domain-specific work to the relevant specialist

## 8. Repository Maintenance Policy: Custom Instructions, Agents, and Prompts

### Overview
All contributors (human and AI agents) working on GitHub Issues or Pull Requests **MUST** reflect on whether their work requires updates to:
- **Repository custom instructions** (`AGENTS.md`)
- **Custom Copilot Agents** (`.agent.md` files in `.github/agents/`)
- **Prompt files** (`.prompt.md` files in `.github/prompts/`)

### When to Update These Resources

#### Update AGENTS.md When:
- You discover new patterns, conventions, or technical constraints that should be documented
- Existing instructions are incorrect, incomplete, or outdated
- You repeatedly face the same issue that could be prevented with better instructions
- You make architectural or infrastructure changes that affect how agents should work

#### Create/Update Custom Agents When:
- You perform a specialized, repeatable task that requires domain-specific knowledge
- The task involves a complex workflow that could be automated
- Multiple issues require similar specialized expertise
- The task benefits from having its own context and tool access

#### Create/Update Prompt Files When:
- You discover a useful pattern or approach that could be reused
- There's a specific task that benefits from guided instructions
- You want to standardize how certain operations are performed
- The task is well-defined but doesn't require the full complexity of a custom agent

### Reflection Requirement
**Before requesting a PR review or closing an issue**, assignees and authors must reflect on their experience:
1. What challenges or issues did you encounter?
2. Were there repeated tasks that could be automated?
3. Did you discover undocumented patterns or conventions?
4. Would future contributors benefit from codified guidance?
5. Should your approach be captured as a custom agent or prompt file?

### Official Documentation References
- [Repository custom instructions](https://docs.github.com/en/copilot/how-tos/configure-custom-instructions/add-repository-instructions) - Add guidance specific to this repository
- [Custom Copilot Agents](https://docs.github.com/en/copilot/how-tos/use-copilot-agents/coding-agent/create-custom-agents) - Create specialized agents for complex tasks
- [Prompt files](https://docs.github.com/en/copilot/tutorials/customization-library/prompt-files/your-first-prompt-file) - Define reusable prompts for common operations

### Checklist for Issue and PR Templates
The following checklist should be incorporated into issue and PR workflows:

**Maintenance Review Checklist:**
- [ ] I have reviewed `AGENTS.md` and determined if updates are needed based on my work
- [ ] I have considered whether a custom agent (`.agent.md`) should be created or updated for this type of task
- [ ] I have considered whether a prompt file (`.prompt.md`) should be created or updated for recurring patterns
- [ ] If I identified needed updates to custom instructions/agents/prompts, I have either:
  - [ ] Made those updates as part of this PR, OR
  - [ ] Created a separate issue to track the needed updates

**Note**: Updates to custom instructions, agents, or prompts should be made thoughtfully. When in doubt, create an issue to discuss the proposed changes rather than making ad-hoc updates.

## Acceptance Checklist for Agents

Before completing a task, verify:
- [ ] Change builds successfully (`dotnet build`)
- [ ] All tests pass (`dotnet test`)
- [ ] Infrastructure validates (if Bicep changed)
- [ ] No hardcoded secrets or credentials introduced
- [ ] RBAC guidelines followed (if identity/auth changed)
- [ ] Observability intent preserved (if logging/telemetry changed)
- [ ] Work is scriptable via CLI where possible (document Portal steps if necessary)
