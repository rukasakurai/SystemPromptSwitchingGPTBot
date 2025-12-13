# README.next

- Install prerequisites
- Sign in: `az login`.
- Create Bot identity (Entra app registration): **Single-tenant** app + **client secret**.
- Provision Azure resources: `azd provision`.
- Deploy the app
- In Azure Bot resource: enable **Teams** channel.
- Teams app package:
  - Edit `manifest/manifest.json` placeholders.
  - Zip **only** `manifest.json`, `color.png`, `outline.png` (no parent folder).
  - Upload/install in Teams (personal sideloading or org admin upload).
