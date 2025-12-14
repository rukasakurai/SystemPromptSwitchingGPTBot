# AI Agent Collaboration Contract

This document establishes the contract between this repository and AI agents. It externalizes assumptions that materially affect correctness, security, cost, and debuggability.

## 1. Canonical Execution Environment

### Reference Platforms
- **Supported OS**: Windows and Linux (both fully supported for CI and local development)
  - .NET and Azure Web Apps have first-class support on both platforms
  - CI: Windows (for .NET tests), Linux (for Bicep validation)
  - Local: Windows is widely used by contributors; Linux is equally supported
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

### Blessed Path for Setup
```shell
# Cross-platform commands (work identically on Windows PowerShell/pwsh and Linux bash)

# 1. Prerequisites check
dotnet --version  # Must be 8.0.x or higher

# 2. Restore dependencies
dotnet restore ./app/SystemPromptSwitchingGPTBot.csproj
dotnet restore ./tests/SystemPromptSwitchingGPTBot.Tests.csproj

# 3. Build
dotnet build ./app/SystemPromptSwitchingGPTBot.csproj --configuration Release

# 4. Run tests
dotnet test ./tests --configuration Release

# 5. For local bot testing, configure Azure OpenAI access
az login
# Edit app/appsettings.Development.json with your Azure OpenAI endpoint and deployment name
dotnet run --project ./app
```

## 2. Repeatable Setup Invariants

### CLI/Code-First Principle
- **All setup steps MUST be scriptable** via CLI commands or code
- Configuration is declarative in:
  - `.csproj` files for dependencies
  - `appsettings.json` / `appsettings.Development.json` for app config
  - Bicep files (`infra/*.bicep`) for Azure resources
- **Never** introduce steps that require GUI interaction to reproduce

### Declarative Infrastructure as Source of Truth
- **Bicep files in `infra/` are authoritative** for Azure resource definitions
- Infrastructure changes MUST be made in Bicep, not via Portal
- Main deployment entry point: `infra/main.bicep`
- Module structure:
  - `platform.bicep` - Reusable platform resources (App Service Plan, OpenAI, App Insights)
  - `app.bicep` - Bot-specific resources (Web App, Bot Service)
  - `role.bicep` - RBAC assignments
  
### GUI/Portal Usage Policy
- **Portal is for exploration and verification ONLY**
- Do not document Portal-only procedures
- Any configuration discovered via Portal MUST be translated to Bicep or app config

## 3. Agent Work Validation Commands

### Install/Bootstrap
```shell
# Cross-platform commands (work identically on both platforms)
dotnet restore ./app/SystemPromptSwitchingGPTBot.csproj
dotnet restore ./tests/SystemPromptSwitchingGPTBot.Tests.csproj
```

### Build
```shell
# Build application
dotnet build ./app/SystemPromptSwitchingGPTBot.csproj --configuration Release
```

```powershell
# Validate infrastructure - Windows PowerShell
cd infra; bicep build main.bicep
```

```bash
# Validate infrastructure - Linux/bash or pwsh
cd infra && bicep build main.bicep
```

### Test
```shell
# Run all tests (cross-platform)
dotnet test ./tests --configuration Release

# Run with detailed output
dotnet test ./tests --configuration Release --verbosity normal
```

### Lint/Format

**Bicep linting:**
```powershell
# Windows PowerShell - validate each Bicep file
cd infra; Get-ChildItem *.bicep | ForEach-Object { bicep build $_.Name }
```

```bash
# Linux/bash or pwsh - validate each Bicep file
cd infra && for file in *.bicep; do bicep build "$file"; done
```

**.NET code formatting:**
```shell
# No .editorconfig or formatting rules currently configured
# If adding code style rules in the future, use:
dotnet format ./app/SystemPromptSwitchingGPTBot.csproj --verify-no-changes
```

### Minimum Bar for "Change is Acceptable"
1. `dotnet build` succeeds with no errors
2. `dotnet test` passes all existing tests
3. Bicep files validate successfully (if infrastructure changed)
4. No new security vulnerabilities introduced
5. Application still runs locally and connects to Bot Framework Emulator

## 4. Azure / Entra Identity Invariants (MUST-NOT-VIOLATE)

### App Registrations vs Service Principals
- **App Registrations define applications** - they are the identity definition
- **Service Principals are the security principals** - they are the runtime identity in a tenant
- When an App Registration is created, a Service Principal is automatically created in the tenant
- **Enterprise Applications** in Portal are the SP representation - use for viewing, NOT creating

### RBAC Assignment Rules
- **ALWAYS assign RBAC to Service Principals, NEVER to App Registrations**
- For Azure OpenAI: Assign "Cognitive Services OpenAI User" role to:
  - Web App's Managed Identity (system-assigned)
  - Developer's user identity (for local dev)
  - **NOT** to the Bot Framework App Registration
  
### Identity Types by Use Case
- **Bot Framework identity**: App Registration with client secret (for Bot ↔ Bot Service auth)
  - Config: `MicrosoftAppId`, `MicrosoftAppPassword`, `MicrosoftAppTenantId`
  - Type: `SingleTenant` (recommended for single-tenant scenarios; `MultiTenant` is still valid for cross-tenant scenarios)
- **Web App to Azure OpenAI**: Managed Identity (system-assigned, passwordless)
  - Web App's identity gets RBAC on OpenAI resource
  - Uses `DefaultAzureCredential` in code
- **Local dev to Azure OpenAI**: User identity via `az login`
  - Developer's user account gets RBAC on OpenAI resource

### Common Mistakes to AVOID
- ❌ Assigning RBAC to an App Registration's Object ID
- ❌ Using user accounts for non-interactive workloads
- ❌ Creating multiple App Registrations when Managed Identity suffices
- ❌ Storing credentials in code or appsettings.json (use Key Vault or managed identity)

## 5. Observability Model Invariants

### Current Approach
- **Platform auto-instrumentation**: Application Insights configured at Web App level
- **Connection string**: Passed via environment variable `APPLICATIONINSIGHTS_CONNECTION_STRING`
- **SDK usage**: Optional in-app SDKs can enhance telemetry (not currently implemented beyond platform defaults)

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
- Application Insights resource MUST be in `infra/platform.bicep`
- Web App MUST reference App Insights connection string in `infra/app.bicep`
- Any new observability tools MUST be provisioned via Bicep, not Portal

### Observability Changes Require Confirmation
- Before changing observability approach (e.g., adding Serilog, OpenTelemetry), **ask the user**
- Significant changes affect cost, debuggability, and compliance requirements

## 6. Change Guardrails and Authority Boundaries

### What Agents MAY Do
✅ Scaffold new bot dialogs or GPT configurations  
✅ Refactor code within existing architectural boundaries  
✅ Fix bugs and add tests for existing functionality  
✅ Improve error handling and logging  
✅ Update dependencies to patch versions (e.g., 1.2.3 → 1.2.4)  
✅ Add Bicep resources that fit current architecture  
✅ Optimize existing code for performance or readability  

### What Agents MUST NOT Do Without Explicit Confirmation
🛑 Add new NuGet packages (especially major libraries)  
🛑 Change authentication model (e.g., switching from managed identity to keys)  
🛑 Modify identity types or RBAC patterns  
🛑 Change observability stack (e.g., replacing Application Insights)  
🛑 Introduce breaking changes to Bot Framework message handling  
🛑 Change deployment targets (e.g., from Web App to Container Apps)  
🛑 Modify `main.bicep` parameters that affect security or cost  

### When Unclear: ASK
- If a task could affect architecture, security, cost, or debuggability: **Stop and ask**
- Provide options with trade-offs instead of making unilateral decisions
- Examples of "ask first" scenarios:
  - "Should I add Entity Framework Core for conversation state persistence?"
  - "Should I switch to Azure OpenAI Assistants API instead of Chat Completions?"
  - "Should I add Redis for distributed state storage?"

## 7. What AGENTS.md Cannot Solve (and Mitigations)

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

## 8. Documentation Guidelines

- Documentation (e.g., README.md) should be **very concise**
- New documentation should be **rarely added** unless it prevents repeated human intervention
- **Correcting incorrect documentation is encouraged**
- If adding docs, ensure they follow the CLI/code-first principle

## Acceptance Checklist for Agents

Before completing a task, verify:
- [ ] Change builds successfully (`dotnet build`)
- [ ] All tests pass (`dotnet test`)
- [ ] Infrastructure validates (if Bicep changed)
- [ ] No hardcoded secrets or credentials introduced
- [ ] RBAC rules followed (if identity/auth changed)
- [ ] Observability intent preserved (if logging/telemetry changed)
- [ ] Work is repeatable via CLI (no GUI-only steps)
- [ ] Change aligns with authority boundaries (or was confirmed by user)
