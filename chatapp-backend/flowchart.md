graph TD;
    A[Client Request] --> B{ChatController}
    B -->|Validate Input| C[Check Parameters]
    C -->|Valid| D[Process Request]
    C -->|Invalid| E[Return Error]
    D -->|Generate Chat| F{IOpenAIService}
    F --> G[OpenAIService]
    G --> H[Generate Response]
    H --> I[Return Response]
    D -->|Save/Retrieve Data| J{ICosmosDbService}
    J --> K[CosmosDbService]
    K --> L[Save Session: userId, sessionId]
    K --> M[Get Session: userId, sessionId]
    K --> N[Get Sessions for User: userId]
    L --> O[Persist to Cosmos DB]
    M --> P[Retrieve from Cosmos DB]
    N --> Q[Retrieve All Sessions]
    O --> R[Return Success]
    P --> R
    Q --> R
    I --> S[Send Response to Client]
    R --> S
    E --> S
