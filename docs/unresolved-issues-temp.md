# Temporary: Unresolved Issues Summary

This file is temporary and should be removed before merging to `main`.

## Current Deployment Context
- Web App: https://web-oaokc6w4inur2.azurewebsites.net/
- Bot Service endpoint: /api/messages (Direct Line POSTs return 200)
- Azure OpenAI: oai-oaokc6w4inur2; deployment: gpt-4o
- Bot App (msaAppId): 916985ca-fa45-4bdf-af81-3a2ee00a00c3

## Unresolved Issues
- **Conditional Access block (AADSTS53003):**
  - Symptom: Outbound replies fail; logs show claims challenge and token acquisition failure for client credentials.
  - Impact: Bot receives messages (200 on /api/messages) but cannot send replies to channel.
  - Next actions: Check Entra ID → Sign-in logs → Service principal sign-ins for the bot app; identify the blocking CA policy (from claims challenge/capolids) and adjust/exclude for app-only token issuance.

- **Azure OpenAI connectivity failure (DNS/host resolution):**
  - Symptom: `No such host is known (oai-oaokc6w4inur2.openai.azure.com:443)` in App Service logs.
  - Impact: Bot cannot call the model; reply pipeline stalls.
  - Next actions: Verify `OpenAIEndpoint` app setting and resource name; check networking (VNet/private endpoints, DNS) and ensure public endpoint is reachable from App Service.

## Completed (for reference)
- App Service diagnostics logging enabled; INFO logs visible (including incoming text).
- Fail-soft handling added: controller avoids 500s and `OnTurnError` swallows send failures.
- Teams manifest corrected to the deployed bot App ID; Teams channel enabled on Bot Service.

## Removal Plan
- Delete this file before merging to `main`: docs/unresolved-issues-temp.md
- Optional: add a short PR checklist item to confirm removal.
