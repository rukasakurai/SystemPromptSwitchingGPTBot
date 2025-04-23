# Sequence Diagram

This sequence diagram represents a typical user scenario of using the app with the specified entities, including authentication flows.

```mermaid
sequenceDiagram
    participant User as Teams Client
    participant TeamsService as Teams Service
    participant BotService as Azure Bot Service
    participant Bot as Bot App (App Service)
    participant Entra as Microsoft Entra ID

    %% User is already authenticated to Teams (happens before bot interaction)
    Note over User,TeamsService: User is already authenticated to Teams<br/>(separate from bot authentication)

    %% Service-to-service authentication for securing API endpoints
    Note over Bot,Entra: Service-to-service authentication<br/>to secure bot endpoints
    Bot->>Entra: Request access token with<br/>MicrosoftAppId & MicrosoftAppPassword
    Entra-->>Bot: Return service access token for Bot Service communication
    Note over BotService,Bot: Bot endpoint (/api/messages) is secured<br/>and only accepts requests with valid tokens

    %% Initial message flow
    User->>TeamsService: Send message
    TeamsService->>BotService: Forward message with Teams auth context
    BotService->>Bot: Deliver activity with auth header<br/>(using Bot Framework JWT)

    %% Bot validates incoming requests
    Bot->>Bot: Validate request is from legitimate Bot Service<br/>using Bot Framework Authentication

    %% Main flow - optional user authentication to access external resources
    alt Bot needs to access resources on user's behalf
        %% This is separate from Teams authentication
        Note over Bot: Bot determines user needs to authenticate<br/>to access specific resources
        Bot->>TeamsService: Send OAuthCard with sign-in button
        TeamsService->>User: Display sign-in card
        User->>TeamsService: Click "Sign in"
        TeamsService->>Entra: Start OAuth flow (browser popup)
        User->>Entra: Authenticate and consent to specific permissions
        Entra-->>TeamsService: Return auth code
        TeamsService->>BotService: Send TokenResponse activity
        BotService->>Bot: Deliver TokenResponse
        Bot->>Entra: Exchange code for tokens
        Entra-->>Bot: Return access & refresh tokens
        Bot->>Bot: Store user's token in bot state<br/>for accessing resources on user's behalf
    end

    %% Main message processing flow
    Bot->>Bot: Process message
    Bot->>BotService: Send response message (with bot's service token)
    BotService->>TeamsService: Forward response
    TeamsService->>User: Display bot's response

    %% Token refresh (optional)
    opt When user's resource access token expires
        Bot->>Entra: Refresh token request
        Entra-->>Bot: New access token
        Bot->>Bot: Update stored token
    end
```

## Authentication Notes

- **Teams Authentication**: The user authenticates to Teams independently before any bot interaction occurs
- **Bot Service Authentication**: The bot endpoint is secured using the app registration (MicrosoftAppId & MicrosoftAppPassword)
- **User Resource Authentication**: Optional flow that only happens when the bot needs to access resources on behalf of the user

## Endpoints

- Teams Client: N/A
- Teams Service: N/A
- Azure Bot Service: N/A
- Bot App (App Service): `https://<your-app-service-name>.azurewebsites.net/api/messages` (secured endpoint)
- Microsoft Entra ID: `https://login.microsoftonline.com/<tenant-id>`
