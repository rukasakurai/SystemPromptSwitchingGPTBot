# Sequence Diagram

This sequence diagram represents a typical user scenario of using the app with the specified entities.

```mermaid
sequenceDiagram
    participant User as Teams Client
    participant TeamsService as Teams Service
    participant BotService as Azure Bot Service
    participant Bot as Bot App (App Service)
    participant Entra as Microsoft Entra ID
    participant OpenAI as Azure OpenAI Service

    %% User is already authenticated to Teams (happens before bot interaction)
    Note over User,TeamsService: User is already authenticated to Teams<br/>(separate from bot authentication)

    %% Service-to-service authentication for securing API endpoints
    Note over Bot,Entra: Service-to-service authentication<br/>to secure bot endpoints
    Bot->>Entra: Request access token with<br/>MicrosoftAppId & MicrosoftAppPassword (HTTPS)
    Entra-->>Bot: Return service access token for Bot Service communication (HTTPS)
    Note over BotService,Bot: Bot endpoint (/api/messages) is secured<br/>and only accepts requests with valid tokens

    %% Initial message flow
    User->>TeamsService: Send message (HTTPS)
    TeamsService->>BotService: Forward message with Teams auth context (HTTPS)
    BotService->>Bot: Deliver activity with auth header<br/>(HTTPS POST to /api/messages endpoint)

    %% Bot validates incoming requests
    Bot->>Bot: Validate request is from legitimate Bot Service<br/>using Bot Framework Authentication

    %% Main message processing flow
    Bot->>Bot: Process message (access conversation state)

    %% Azure OpenAI interaction
    Note over Bot,OpenAI: Bot uses DefaultAzureCredential<br/>for Azure OpenAI authentication
    Bot->>OpenAI: Send chat completion request with system prompt (HTTPS)
    OpenAI-->>Bot: Return generated response (HTTPS)

    Bot->>BotService: Send response message with bot's service token (HTTPS)
    BotService->>TeamsService: Forward response (HTTPS)
    TeamsService->>User: Display bot's response (HTTPS/WebSocket)
```

## Communication Protocols

- **HTTPS**: Used for all secure API communication between services
- **WebSocket**: Used for real-time communication between Teams client and service for message delivery
- **JWT (JSON Web Tokens)**: Used for authentication between services (carried over HTTPS)

## Authentication Notes

- **Teams Authentication**: The user authenticates to Teams independently before any bot interaction occurs
- **Bot Service Authentication**: The bot endpoint is secured using the app registration (MicrosoftAppId & MicrosoftAppPassword)
- **No User Resource Authentication**: This bot implementation does not include functionality for the bot to access resources on behalf of the user (no OAuthCard, token exchange, etc.)
- **Azure OpenAI Authentication**: The bot uses DefaultAzureCredential (managed identity) to authenticate with Azure OpenAI Service

## Endpoints

- Teams Client: N/A
- Teams Service: N/A
- Azure Bot Service: N/A
- Bot App (App Service): `https://<your-app-service-name>.azurewebsites.net/api/messages` (secured endpoint)
- Microsoft Entra ID: `https://login.microsoftonline.com/<tenant-id>`
- Azure OpenAI Service: `https://<your-openai-resource-name>.openai.azure.com/`
