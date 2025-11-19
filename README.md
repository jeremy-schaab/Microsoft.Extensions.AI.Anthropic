# Microsoft.Extensions.AI.Anthropic

[![Build Status](https://img.shields.io/badge/build-pending-lightgrey)](https://github.com/jeremy-schaab/Microsoft.Extensions.AI.Anthropic)
[![License](https://img.shields.io/badge/license-MIT-blue)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%209.0-purple)](https://dotnet.microsoft.com/download)

A .NET library that integrates Anthropic's Claude AI models with the [Microsoft.Extensions.AI](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai) abstractions framework, enabling standardized AI client interactions across different providers.

## Overview

This library provides an `IChatClient` implementation for Anthropic's Claude models, supporting both:

- **PRIMARY**: Azure Anthropic Foundry (`AnthropicFoundryClient`) - Azure-hosted Anthropic API with enterprise authentication
- **SECONDARY**: Standard Anthropic API (`AnthropicClient`) - Direct Anthropic API access

By implementing the Microsoft.Extensions.AI abstractions, this library enables you to:

- Use Claude models through a standardized interface
- Switch between AI providers without changing application code
- Leverage middleware patterns (caching, telemetry, retries)
- Integrate with dependency injection seamlessly
- Support Azure authentication (Managed Identity, DefaultAzureCredential, API Keys)

## Table of Contents

- [Features](#features)
- [Installation](#installation)
- [Quick Start](#quick-start)
  - [Azure Foundry (Recommended for Production)](#azure-foundry-recommended-for-production)
  - [Standard Anthropic API](#standard-anthropic-api)
- [Authentication](#authentication)
- [Usage Examples](#usage-examples)
  - [Basic Chat](#basic-chat)
  - [Streaming Responses](#streaming-responses)
  - [Tool Calling](#tool-calling)
  - [Multi-Modal (Vision)](#multi-modal-vision)
- [Dependency Injection](#dependency-injection)
- [Configuration](#configuration)
- [Supported Models](#supported-models)
- [Architecture](#architecture)
- [Documentation](#documentation)
- [Building and Testing](#building-and-testing)
- [Contributing](#contributing)
- [License](#license)

## Features

- **Azure Foundry Integration**: First-class support for Azure-hosted Anthropic API with enterprise authentication
- **IChatClient Implementation**: Full implementation of Microsoft.Extensions.AI abstractions
- **Streaming Support**: Real-time token streaming with event aggregation
- **Tool/Function Calling**: Native support for Claude's tool use capabilities
- **Multi-Modal**: Support for images and PDFs (Claude 3+ models)
- **System Messages**: Automatic handling of system message extraction
- **Azure Authentication**: Support for API Keys, Bearer Tokens, and Azure Identity (Managed Identity, DefaultAzureCredential)
- **Dependency Injection**: Built-in extensions for IServiceCollection and IChatClientBuilder
- **Type Conversion**: Seamless conversion between Microsoft.Extensions.AI and Anthropic SDK types
- **Extended Thinking**: Support for Claude Opus 4's reasoning traces

## Installation

Install the NuGet package (coming soon):

```bash
dotnet add package Microsoft.Extensions.AI.Anthropic
```

### Required Dependencies

The package includes dependencies on:

- `Anthropic.Foundry` (0.0.1+) - Azure Anthropic Foundry client
- `Anthropic` (10.1.2+) - Standard Anthropic SDK
- `Azure.Identity` (1.17.0+) - Azure authentication
- `Microsoft.Extensions.AI.Abstractions` - Core abstractions

## Quick Start

### Azure Foundry (Recommended for Production)

Use Azure-hosted Anthropic API with enterprise authentication:

```csharp
using Microsoft.Extensions.AI;
using Anthropic.Foundry;

// Option 1: Azure Identity (DefaultAzureCredential - recommended)
var credentials = await IAnthropicFoundryCredentials.FromEnv();
var foundryClient = new AnthropicFoundryClient(credentials);
IChatClient chatClient = new AnthropicChatClient(foundryClient);

// Option 2: API Key authentication
var credentials = new AnthropicFoundryApiKeyCredentials(
    apiKey: "your-api-key",
    resourceName: "your-azure-resource"
);
var foundryClient = new AnthropicFoundryClient(credentials);
IChatClient chatClient = new AnthropicChatClient(foundryClient);

// Use the client
var response = await chatClient.GetResponseAsync(
    [new ChatMessage(ChatRole.User, "What is the capital of France?")],
    new ChatOptions { ModelId = "claude-sonnet-4-5" });

Console.WriteLine(response.Message.Text);
```

#### Environment Variables for Azure Foundry

Set these environment variables for automatic configuration:

```bash
# Required
ANTHROPIC_FOUNDRY_RESOURCE=your-azure-resource-name

# Optional - if not set, uses DefaultAzureCredential
ANTHROPIC_FOUNDRY_API_KEY=your-api-key
```

### Standard Anthropic API

Use the standard Anthropic API directly:

```csharp
using Microsoft.Extensions.AI;
using Anthropic;

var anthropicClient = new AnthropicClient(new ClientOptions
{
    APIKey = "sk-ant-..."
});

IChatClient chatClient = new AnthropicChatClient(anthropicClient);

var response = await chatClient.GetResponseAsync(
    [new ChatMessage(ChatRole.User, "Hello, Claude!")]);

Console.WriteLine(response.Message.Text);
```

## Authentication

### Azure Foundry Authentication Methods

#### 1. Azure Identity (Recommended for Production)

Uses `DefaultAzureCredential` which automatically tries multiple authentication methods:

```csharp
// Automatically uses: Environment variables, Managed Identity, Visual Studio, Azure CLI, etc.
var credentials = await IAnthropicFoundryCredentials.FromEnv();
var client = new AnthropicFoundryClient(credentials);
```

#### 2. API Key Authentication

For development or specific scenarios:

```csharp
var credentials = new AnthropicFoundryApiKeyCredentials(
    apiKey: "your-api-key",
    resourceName: "your-azure-resource"
);
var client = new AnthropicFoundryClient(credentials);
```

#### 3. Bearer Token Authentication

For custom token-based authentication:

```csharp
var credentials = new AnthropicFoundryBearerTokenCredentials(
    apiKey: "your-bearer-token",
    resourceName: "your-azure-resource"
);
var client = new AnthropicFoundryClient(credentials);
```

### Standard API Authentication

Simply provide your Anthropic API key:

```csharp
var client = new AnthropicClient(new ClientOptions
{
    APIKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")
});
```

See [AUTHENTICATION.md](docs/AUTHENTICATION.md) for detailed authentication guidance.

## Usage Examples

### Basic Chat

```csharp
using Microsoft.Extensions.AI;

var messages = new List<ChatMessage>
{
    new(ChatRole.System, "You are a helpful assistant."),
    new(ChatRole.User, "Explain quantum computing in simple terms.")
};

var options = new ChatOptions
{
    ModelId = "claude-sonnet-4-5",
    MaxOutputTokens = 1024,
    Temperature = 0.7f
};

var response = await chatClient.GetResponseAsync(messages, options);

Console.WriteLine(response.Message.Text);
Console.WriteLine($"Tokens used: {response.Usage?.TotalTokenCount}");
```

### Streaming Responses

Stream tokens in real-time for better user experience:

```csharp
var messages = new[]
{
    new ChatMessage(ChatRole.User, "Write a short story about a robot.")
};

await foreach (var update in chatClient.GetStreamingResponseAsync(messages))
{
    foreach (var content in update.Contents.OfType<TextContent>())
    {
        Console.Write(content.Text);
    }
}
```

### Tool Calling

Enable Claude to call functions/tools:

```csharp
using Microsoft.Extensions.AI;

// Define a tool
var getWeatherTool = AIFunctionFactory.Create(
    (string location) => $"The weather in {location} is sunny, 72°F",
    name: "get_weather",
    description: "Get the current weather for a location"
);

var options = new ChatOptions
{
    ModelId = "claude-sonnet-4-5",
    Tools = [getWeatherTool],
    ToolMode = AutoChatToolMode.Instance
};

var messages = new[]
{
    new ChatMessage(ChatRole.User, "What's the weather in San Francisco?")
};

var response = await chatClient.GetResponseAsync(messages, options);

// Check for tool calls
foreach (var toolCall in response.Message.Contents.OfType<FunctionCallContent>())
{
    Console.WriteLine($"Tool called: {toolCall.Name}");
    Console.WriteLine($"Arguments: {toolCall.Arguments}");
}
```

### Multi-Modal (Vision)

Send images to Claude for analysis:

```csharp
using Microsoft.Extensions.AI;

// Load image
byte[] imageData = await File.ReadAllBytesAsync("diagram.png");

var message = new ChatMessage(ChatRole.User,
[
    new TextContent("What is in this image?"),
    new DataContent(imageData, "image/png")
]);

var response = await chatClient.GetResponseAsync(
    [message],
    new ChatOptions { ModelId = "claude-sonnet-4-5" });

Console.WriteLine(response.Message.Text);
```

## Dependency Injection

### Azure Foundry with DI

Register the chat client with dependency injection:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.AI;

var services = new ServiceCollection();

// Option 1: From environment variables
services.AddAnthropicFoundryChatClientFromEnvironment(
    modelId: "claude-sonnet-4-5"
);

// Option 2: With explicit credentials
var credentials = new AnthropicFoundryApiKeyCredentials(
    apiKey: "your-api-key",
    resourceName: "your-azure-resource"
);
services.AddAnthropicFoundryChatClient(credentials, modelId: "claude-sonnet-4-5");

// Option 3: With configuration
services.AddAnthropicFoundryChatClient(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var credentials = new AnthropicFoundryApiKeyCredentials(
        apiKey: config["Azure:Anthropic:ApiKey"],
        resourceName: config["Azure:Anthropic:ResourceName"]
    );
    return new AnthropicFoundryClient(credentials);
});

// Use in application
var provider = services.BuildServiceProvider();
var chatClient = provider.GetRequiredService<IChatClient>();
```

### Standard API with DI

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.AI;

var services = new ServiceCollection();

// Simple registration
services.AddAnthropicChatClient(
    apiKey: "sk-ant-...",
    modelId: "claude-sonnet-4-5"
);

// With configuration
services.AddAnthropicChatClient(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    return new AnthropicClient(new ClientOptions
    {
        APIKey = config["Anthropic:ApiKey"]
    });
});
```

### IChatClientBuilder Pattern

Use the builder pattern for more control:

```csharp
var builder = new ChatClientBuilder();

builder.AddAnthropicFoundryChatClient(credentials, modelId: "claude-sonnet-4-5")
       .UseCaching()            // Add caching middleware
       .UseOpenTelemetry()      // Add telemetry
       .UseRetryPolicy();       // Add retry logic

IChatClient chatClient = builder.Build();
```

## Configuration

### ChatOptions Properties

Configure each request with `ChatOptions`:

```csharp
var options = new ChatOptions
{
    ModelId = "claude-sonnet-4-5",           // Model to use
    MaxOutputTokens = 2048,                  // Maximum tokens to generate
    Temperature = 0.7f,                      // Randomness (0.0 - 1.0)
    TopP = 0.9f,                             // Nucleus sampling
    TopK = 40,                               // Top-K sampling
    StopSequences = ["STOP", "END"],         // Stop generation sequences
    Tools = [weatherTool, searchTool],       // Available tools
    ToolMode = AutoChatToolMode.Instance,    // Tool usage mode
    Metadata = new()                         // Additional metadata
    {
        ["user_id"] = "12345"
    }
};
```

### System Messages

System messages are automatically extracted and sent via Anthropic's `system` parameter:

```csharp
var messages = new[]
{
    new ChatMessage(ChatRole.System, "You are a helpful coding assistant."),
    new ChatMessage(ChatRole.System, "Always provide code examples."),
    new ChatMessage(ChatRole.User, "How do I sort an array in C#?")
};

// Both system messages are combined and sent as the system parameter
var response = await chatClient.GetResponseAsync(messages);
```

## Supported Models

The library supports all Claude models available through Anthropic:

### Latest Models (as of 2025)

| Model ID | Description | Context Window | Best For |
|----------|-------------|----------------|----------|
| `claude-sonnet-4-5` | Claude 3.5 Sonnet (latest) | 200K tokens | General purpose, best balance |
| `claude-opus-4` | Claude Opus 4 | 200K tokens | Complex reasoning, extended thinking |
| `claude-haiku-4` | Claude Haiku 4 | 200K tokens | Fast, lightweight tasks |

### Legacy Models

- `claude-3-5-sonnet-20241022` - Claude 3.5 Sonnet (Oct 2024)
- `claude-3-opus-20240229` - Claude 3 Opus
- `claude-3-sonnet-20240229` - Claude 3 Sonnet
- `claude-3-haiku-20240307` - Claude 3 Haiku

**Note**: Model availability may vary between Azure Foundry and standard API. Always verify model IDs with your deployment.

## Architecture

This implementation follows the established pattern from `Microsoft.Extensions.AI.OpenAI`:

```
┌─────────────────────────────────────────────────────────────┐
│                   Your Application                          │
│         (Uses Microsoft.Extensions.AI abstractions)         │
└───────────────────────┬─────────────────────────────────────┘
                        │ IChatClient interface
┌───────────────────────▼─────────────────────────────────────┐
│            AnthropicChatClient (This Library)               │
│  ┌─────────────────────────────────────────────────────┐    │
│  │ Type Converters                                     │    │
│  │  • ChatMessage ↔ MessageParam                       │    │
│  │  • ChatOptions ↔ MessageCreateParams                │    │
│  │  • AIContent ↔ ContentBlock (text, image, tool)     │    │
│  │  • Streaming Event Aggregation                      │    │
│  └─────────────────────────────────────────────────────┘    │
└───────────────────────┬─────────────────────────────────────┘
                        │
        ┌───────────────┴──────────────┐
        │                               │
┌───────▼──────────┐          ┌────────▼────────────┐
│ AnthropicClient  │          │AnthropicFoundryClient│
│  (Standard API)  │          │   (Azure Foundry)    │
└───────┬──────────┘          └────────┬────────────┘
        │                               │
┌───────▼──────────┐          ┌────────▼────────────┐
│ api.anthropic.com│          │ *.ai.azure.com      │
└──────────────────┘          └─────────────────────┘
```

### Key Components

- **AnthropicChatClient**: Main `IChatClient` implementation
- **Converters**: Bidirectional type conversion between abstractions and SDK types
- **Extensions**: Dependency injection registration methods
- **Utilities**: Model mapping, exception handling, credential helpers

## Documentation

- **[Azure Setup Guide](docs/AZURE-SETUP.md)**: Setting up Azure Anthropic Foundry resources
- **[Authentication Guide](docs/AUTHENTICATION.md)**: Detailed authentication options and scenarios
- **[Migration Guide](docs/MIGRATION.md)**: Migrating from direct Anthropic SDK usage
- **[Research Plan](docs/research/anthropic-integration-research-plan.md)**: Complete architecture analysis and implementation plan

## Building and Testing

### Prerequisites

- .NET 8.0 SDK or .NET 9.0 SDK
- Visual Studio 2022 or VS Code
- (Optional) Azure subscription for Azure Foundry testing

### Build

```bash
# Restore dependencies and build
dotnet build

# Build in release mode
dotnet build -c Release
```

### Run Tests

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/Microsoft.Extensions.AI.Anthropic.Tests/

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

### Run Examples

```bash
# Azure Foundry basic example
dotnet run --project examples/AzureFoundryBasicExample/

# Standard API example
dotnet run --project examples/BasicChatExample/

# Streaming example
dotnet run --project examples/StreamingChatExample/

# Tool calling example
dotnet run --project examples/ToolCallingExample/
```

### Create NuGet Package

```bash
dotnet pack -c Release -o ./artifacts
```

## Project Structure

```
Microsoft.Extensions.AI.Anthropic/
├── src/
│   └── Microsoft.Extensions.AI.Anthropic/           # Main library
│       ├── AnthropicChatClient.cs                   # IChatClient implementation
│       ├── Converters/                              # Type conversion logic
│       ├── Extensions/                              # DI extensions
│       └── Utilities/                               # Helper utilities
├── tests/
│   └── Microsoft.Extensions.AI.Anthropic.Tests/     # Unit and integration tests
├── examples/
│   ├── AzureFoundryBasicExample/                    # Azure Foundry examples
│   ├── BasicChatExample/                            # Standard API examples
│   ├── StreamingChatExample/                        # Streaming examples
│   ├── ToolCallingExample/                          # Tool calling examples
│   └── VisionExample/                               # Multi-modal examples
├── docs/                                            # Documentation
│   ├── AZURE-SETUP.md                               # Azure setup guide
│   ├── AUTHENTICATION.md                            # Auth guide
│   └── MIGRATION.md                                 # Migration guide
└── README.md                                        # This file
```

## Contributing

Contributions are welcome! Please follow these guidelines:

1. **Fork** the repository
2. **Create a feature branch**: `git checkout -b feature/amazing-feature`
3. **Write tests** for new functionality
4. **Ensure tests pass**: `dotnet test`
5. **Follow coding standards**: Use EditorConfig settings
6. **Update documentation** as needed
7. **Submit a pull request**

### Code Quality Standards

- Maintain 90%+ code coverage
- Follow .NET coding conventions
- Include XML documentation for public APIs
- Use async/await patterns consistently
- Ensure thread-safety for all implementations

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Built on the [Microsoft.Extensions.AI](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai) abstractions framework
- Uses the official [Anthropic C# SDK](https://github.com/anthropics/anthropic-sdk-csharp)
- Inspired by the `Microsoft.Extensions.AI.OpenAI` reference implementation
- Azure Foundry integration powered by `Anthropic.Foundry` package

## Support

- **GitHub Issues**: [Report bugs or request features](https://github.com/jeremy-schaab/Microsoft.Extensions.AI.Anthropic/issues)
- **Microsoft.Extensions.AI Docs**: [Official documentation](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai)
- **Anthropic API Docs**: [API reference](https://docs.anthropic.com/en/api)

---

**Status**: This project is in active development. Contributions and feedback are welcome!
