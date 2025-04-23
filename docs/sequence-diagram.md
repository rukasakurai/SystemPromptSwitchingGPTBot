# Sequence Diagram

This sequence diagram represents a typical user scenario of using the app with the specified entities, including authentication flow.

```mermaid
sequenceDiagram
    participant User as Teams Client
    participant TeamsService as Teams Service
    participant BotService as Azure Bot Service
    participant Bot as Bot App (App Service)
    participant MSAL as Bot's MSAL Client
    participant Entra as Microsoft Entra ID

    %% Initial message flow
    User->>TeamsService: Send message
    TeamsService->>BotService: Forward message (HTTP POST)
    BotService->>Bot: Deliver activity (HTTP POST to bot endpoint)

    alt User not authenticated
        %% Authentication flow with OAuthCard
        Bot->>Bot: Check if user has valid token
        Bot->>TeamsService: Send OAuthCard with sign-in button
        TeamsService->>User: Display sign-in card
        User->>TeamsService: Click "Sign in"
        TeamsService->>Entra: Start OAuth flow (browser popup)
        User->>Entra: Authenticate and consent
        Entra-->>TeamsService: Return auth code
        TeamsService->>BotService: Send TokenResponse activity
        BotService->>Bot: Deliver TokenResponse
        Bot->>MSAL: Exchange code for tokens
        MSAL->>Entra: Request tokens with auth code
        Entra-->>MSAL: Return access & refresh tokens
        MSAL->>Bot: Return tokens
        Bot->>Bot: Store token in bot state
    end

    %% Main message processing flow
    Bot->>Bot: Process message with auth context
    Bot->>TeamsService: Send response message
    TeamsService->>User: Display bot's response

    %% Token refresh (optional)
    opt When token expires
        Bot->>MSAL: Request token refresh
        MSAL->>Entra: Refresh token request
        Entra-->>MSAL: New access token
        MSAL-->>Bot: Updated token
    end
```

## Endpoints

- Teams Client: N/A
- Teams Service: N/A
- Azure Bot Service: N/A
- Bot App (App Service): `https://<your-app-service-name>.azurewebsites.net/api/messages`
- Microsoft Entra ID: `https://login.microsoftonline.com/<tenant-id>`
