# Sequence Diagram

This sequence diagram represents a typical user scenario of using the app with the specified entities.

```mermaid
sequenceDiagram
    participant User as Teams Client
    participant TeamsService as Microsoft Teams platform
    participant BotService as Azure Bot Service
    participant Bot as Bot App (App Service)
    participant Entra as Microsoft Entra ID
    participant OpenAI as Azure OpenAI Service

    %% User is already authenticated to Teams (happens before bot interaction)
    Note over User,TeamsService: User is already authenticated to Teams<br/>(separate from bot authentication)

    %% Service-to-service authentication for securing API endpoints
    Note over Bot,Entra: Service-to-service authentication<br/>to secure bot endpoints
    Bot->>Entra: Request access token with<br/>MicrosoftAppId & MicrosoftAppPassword (HTTPS POST)
    Entra-->>Bot: HTTP 200 OK: Return service access token (HTTPS)
    Note over BotService,Bot: Bot endpoint (/api/messages) is secured<br/>and only accepts requests with valid tokens

    %% Initial message flow
    User->>TeamsService: Send message (HTTPS POST)
    TeamsService-->>User: HTTP 200 OK (HTTPS)
    TeamsService->>BotService: Forward message with Teams auth context (HTTPS POST)
    BotService-->>TeamsService: HTTP 202 Accepted (HTTPS)
    BotService->>Bot: Deliver activity with auth header<br/>(HTTPS POST to /api/messages endpoint)
    Bot-->>BotService: HTTP 200 OK (HTTPS)

    %% Bot validates incoming requests
    Bot->>Bot: Validate request is from legitimate Bot Service<br/>using Bot Framework Authentication

    %% Main message processing flow
    Bot->>Bot: Process message (access conversation state)

    %% Azure OpenAI interaction
    Note over Bot,OpenAI: Bot uses DefaultAzureCredential<br/>for Azure OpenAI authentication
    Bot->>OpenAI: Send chat completion request with system prompt (HTTPS POST)
    OpenAI-->>Bot: HTTP 200 OK: Return generated response (HTTPS)

    Bot->>BotService: Send response message with bot's service token (HTTPS POST)
    BotService-->>Bot: HTTP 200 OK (HTTPS)
    BotService->>TeamsService: Forward response (HTTPS POST)
    TeamsService-->>BotService: HTTP 200 OK (HTTPS)
    TeamsService->>User: Display bot's response (HTTPS/WebSocket)

    %% If using HTTPS for response display
    Note over TeamsService,User: If using HTTPS for response display:
    User->>TeamsService: Poll for updates (HTTPS GET)
    TeamsService-->>User: HTTP 200 OK: Return updates (HTTPS)
```

## Participants

| Participant                                                                                    | Description                                                                                                              | Pricing                                                                                                                              |
| ---------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------ |
| [Teams Client](https://learn.microsoft.com/en-us/microsoftteams/get-clients)                   | The Microsoft Teams client application used by end users to interact with the bot.                                       | [Free to download and use on all platforms](https://www.microsoft.com/en-us/microsoft-teams/download-app)                            |
| [Microsoft Teams platform](https://learn.microsoft.com/en-us/microsoftteams/platform/overview) | The backend platform that powers Microsoft Teams, handling message routing and delivery between clients and services.    | [Free tier available with premium options](https://www.microsoft.com/ja-jp/microsoft-teams/compare-microsoft-teams-options)          |
| [Azure Bot Service](https://learn.microsoft.com/en-us/azure/bot-service/bot-service-overview)  | Microsoft's managed service for bot deployment and connectivity to messaging channels including Teams.                   | [Free for standard channels with premium options](https://azure.microsoft.com/en-us/pricing/details/bot-services/)                   |
| [Bot App (App Service)](https://learn.microsoft.com/en-us/azure/app-service/overview)          | The custom bot application deployed to Azure App Service hosting the bot's logic and handling chat interactions.         | [Free and paid tiers available](https://azure.microsoft.com/en-us/pricing/details/app-service/windows/)                              |
| [Microsoft Entra ID](https://learn.microsoft.com/en-us/entra/identity/fundamentals/whatis-id)  | Microsoft's cloud identity service, formerly known as Azure Active Directory, used for authentication and authorization. | [Free tier with premium features available](https://www.microsoft.com/en-us/security/business/microsoft-entra-pricing)               |
| [Azure OpenAI Service](https://learn.microsoft.com/en-us/azure/ai-services/openai/overview)    | Microsoft's cloud-based service providing API access to OpenAI's GPT models, used for generating responses.              | [Pay-per-use model based on tokens and models](https://azure.microsoft.com/en-us/pricing/details/cognitive-services/openai-service/) |

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
- Microsoft Teams platform: N/A
- Azure Bot Service: N/A
- Bot App (App Service): `https://<your-app-service-name>.azurewebsites.net/api/messages` (secured endpoint)
- Microsoft Entra ID: `https://login.microsoftonline.com/<tenant-id>`
- Azure OpenAI Service: `https://<your-openai-resource-name>.openai.azure.com/`
