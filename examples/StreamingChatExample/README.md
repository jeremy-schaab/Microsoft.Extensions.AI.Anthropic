# Streaming Chat Example

This example demonstrates real-time streaming chat functionality using `Microsoft.Extensions.AI.Anthropic`.

## Features

- Real-time Token Streaming: Displays assistant responses as tokens arrive, providing immediate feedback
- Multi-turn Conversation: Maintains conversation history across multiple exchanges
- Dual API Support: Works with both Azure Anthropic Foundry and standard Anthropic API
- Cancellation Handling: Press Ctrl+C to cancel the current response
- Usage Statistics: Shows token counts and finish reason after each response
- Clean Console Output: Color-coded messages for better readability
- Thread-safe: Uses C# 13's new Lock type for console synchronization

## Prerequisites

- .NET 9.0 SDK
- One of the following:
  - Azure Foundry: Azure Anthropic Foundry resource
  - Standard API: Anthropic API key

## Configuration

Option 1: Azure Anthropic Foundry (Recommended for Production)

Set the following environment variables:

ANTHROPIC_FOUNDRY_RESOURCE=your-resource-name
ANTHROPIC_FOUNDRY_API_KEY=your-api-key (optional, uses Azure Identity if not set)

Authentication methods:
1. API Key: Set ANTHROPIC_FOUNDRY_API_KEY
2. Azure Identity: Leave ANTHROPIC_FOUNDRY_API_KEY unset - uses DefaultAzureCredential
   - Managed Identity (in Azure)
   - Azure CLI credentials (local development)
   - Visual Studio credentials (local development)

Option 2: Standard Anthropic API

Set the following environment variable:

ANTHROPIC_API_KEY=your-anthropic-api-key

## Running the Example

Windows (PowerShell):
$env:ANTHROPIC_FOUNDRY_RESOURCE="your-resource-name"
$env:ANTHROPIC_FOUNDRY_API_KEY="your-api-key"
dotnet run

Windows (Command Prompt):
set ANTHROPIC_FOUNDRY_RESOURCE=your-resource-name
set ANTHROPIC_FOUNDRY_API_KEY=your-api-key
dotnet run

Linux/macOS:
export ANTHROPIC_FOUNDRY_RESOURCE="your-resource-name"
export ANTHROPIC_FOUNDRY_API_KEY="your-api-key"
dotnet run

## Interactive Chat Interface

The application displays:

- Colored output for better readability
- Real-time streaming of assistant responses
- Token usage statistics after each response
- Cancellation support (Ctrl+C)