# Numble Chat Application

## Overview
A modern, AI-powered chat application built with .NET 9.0 backend and vanilla JavaScript frontend, leveraging Azure Cosmos DB for data persistence and OpenAI for intelligent responses.

## Architecture Flowcharts

### Frontend Flow
![Frontend Flowchart](./chatapp-frontend/flowchart-Frontend.png)

### Backend Flow
![Backend Flowchart](./chatapp-backend/flowchart-Backend.png)

## Key Features
- Real-time AI-powered chat sessions
- Persistent chat history
- Session management
- Lightweight and responsive design

## Technologies Used
- **Backend**: 
  - .NET 9.0
  - Azure Cosmos DB
  - OpenAI API
- **Frontend**:
  - Vanilla JavaScript
  - HTML5
  - CSS3
- **Authentication**: 
  - Custom User ID mechanism

## Core Components

### Backend
- **ChatController**: Manages chat-related HTTP endpoints
- **CosmosDbService**: Handles database operations
- **OpenAIService**: Generates AI chat responses

### Frontend
- **ChatApp Class**: Manages user interactions
- **Session Management**: Create, load, and delete chat sessions
- **Caching Mechanism**: Local and session storage for performance

## Endpoints

### Chat Endpoints
- `POST /api/chat`: Create or continue chat sessions
- `GET /api/chat/history`: Retrieve user's chat sessions
- `GET /api/chat/sessions/{userId}/{sessionId}`: Get specific session
- `DELETE /api/chat/sessions/{userId}/{sessionId}`: Delete a session

## Getting Started

### Prerequisites
- .NET 9.0 SDK
- Azure Cosmos DB account
- OpenAI API access

### Configuration
1. Set up Azure Key Vault
2. Configure Cosmos DB connection string
3. Add OpenAI API credentials

---

**Developed with ❤️ using .NET, Azure, and OpenAI**
