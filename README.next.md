# README.next

- Install prerequisites
- Sign in: `az login`
- Create/select an `azd` environment:
  - `azd env new <envName>` (or `azd env select <envName>`)
- Create bot identity (Entra app registration + client secret):
  - `pwsh -File ./infra/hooks/preprovision.ps1`
- Provision Azure resources: `azd provision`
  - This uses the `microsoftApp*` values set by `preprovision.ps1`.
  - On first provision, `azd` may still prompt you to confirm infrastructure parameters and the resource group; it persists your answers under `.azure/<env>/`.
- Deploy the app: `azd deploy`
- In Azure Bot resource: enable **Teams** channel
- Teams app package:
  - Edit `manifest/manifest.json` values (at minimum, set `bots[0].botId` (and `copilotAgents.customEngineAgents[0].id` if present) to the deployed bot's Microsoft App ID).
  - Zip **only** `manifest.json`, `color.png`, `outline.png` (no parent folder).
  - Upload/install in Teams (personal sideloading or org admin upload).
