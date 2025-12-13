# README.next

- Install prerequisites
- Sign in: `az login`
- Create/select an `azd` environment:
  - `azd env new <envName>` (or `azd env select <envName>`)
- Provision Azure resources: `azd provision`
  - This runs the `preprovision` hook which creates a new bot identity (Entra app registration + client secret) and sets the `microsoftApp*` environment values used by Bicep.
- Deploy the app: `azd deploy`
- In Azure Bot resource: enable **Teams** channel
- Teams app package:
  - Edit `manifest/manifest.json` values (at minimum, set the bot ID to the deployed bot's Microsoft App ID).
  - Zip **only** `manifest.json`, `color.png`, `outline.png` (no parent folder).
  - Upload/install in Teams (personal sideloading or org admin upload).
