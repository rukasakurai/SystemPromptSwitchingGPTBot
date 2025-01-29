# Sequence Diagram

This sequence diagram represents a typical user scenario of using the app with the specified entities.

```mermaid
sequenceDiagram
    participant User as Microsoft Teams (client-side client)
    participant TeamsServer as Microsoft Teams (server-side)
    participant BotService as Azure Bot Service
    participant AppService as Azure App Service
    participant Entra as Microsoft Entra (for authentication)

    User->>TeamsServer: Send message (HTTP)
    TeamsServer->>BotService: Forward message (HTTP)
    BotService->>Entra: Authenticate user (HTTP)
    Entra-->>BotService: Authentication result (HTTP)
    BotService->>AppService: Forward message (HTTP)
    AppService->>BotService: Process message (HTTP)
    BotService->>TeamsServer: Send response (HTTP)
    TeamsServer->>User: Deliver response (HTTP)
```

## Endpoints

- Microsoft Teams (client-side client): N/A
- Microsoft Teams (server-side): N/A
- Azure Bot Service: N/A
- Azure App Service: `https://<your-app-service-name>.azurewebsites.net/api/messages`
- Microsoft Entra (for authentication): N/A
