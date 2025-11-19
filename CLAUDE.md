# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is the **Microsoft.Extensions.AI.Anthropic** library - a .NET integration that brings Anthropic's Claude AI models to the Microsoft.Extensions.AI abstractions framework.

**Primary Target**: Azure Anthropic Foundry (`AnthropicFoundryClient`) for Azure-hosted Anthropic API
**Secondary Target**: Standard Anthropic API (`AnthropicClient`)

## Architecture

### Core Design Pattern

This implementation follows the established pattern from `Microsoft.Extensions.AI.OpenAI`:

1. **Wrapper Architecture**: `AnthropicChatClient` implements `IChatClient` and wraps the Anthropic SDK clients
2. **Type Conversion Layer**: Bidirectional converters between Microsoft.Extensions.AI types and Anthropic SDK types
3. **Dual Client Support**: Single implementation supports both `AnthropicFoundryClient` (Azure) and `AnthropicClient` (standard API)

### Key Components

```
AnthropicChatClient (implements IChatClient)
  ├── Converters/
  │   ├── AnthropicMessageConverter     - ChatMessage ↔ MessageParam
  │   ├── AnthropicContentConverter     - AIContent ↔ ContentBlock (text, images, tools)
  │   ├── AnthropicOptionsConverter     - ChatOptions ↔ MessageCreateParams
  │   ├── AnthropicToolConverter        - AIFunctionDeclaration ↔ ToolDefinition
  │   └── AnthropicStreamingConverter   - RawMessageStreamEvent → ChatResponseUpdate
  ├── Extensions/
  │   ├── MicrosoftExtensionsAIAnthropicExtensions        - DI for standard API
  │   └── MicrosoftExtensionsAIAnthropicFoundryExtensions - DI for Azure Foundry
  └── Utilities/
      ├── AnthropicModelMapper          - Model ID mapping
      ├── AnthropicExceptionMapper      - Exception translation
      └── AnthropicCredentialsHelper    - Azure credential utilities
```

## Critical Implementation Details

### System Message Handling

**Challenge**: Anthropic uses a separate `system` parameter; M.E.AI includes system messages in the message array.

**Solution**:
- Extract all `ChatRole.System` messages from the message array
- Combine their text content into a single system prompt
- Set `MessageCreateParams.System` parameter
- Exclude system messages from the message array sent to Anthropic

### Streaming Event Aggregation

**Challenge**: Anthropic emits fine-grained SSE events (`message_start`, `content_block_start`, `content_block_delta`, `message_delta`, `message_stop`); M.E.AI expects message-level `ChatResponseUpdate` objects.

**Solution**: Implement state machine in `AnthropicStreamingConverter`:
- Maintain accumulator state for current message
- Accumulate text deltas into StringBuilder
- Track tool calls across multiple content blocks
- Yield `ChatResponseUpdate` when meaningful content accumulated
- Final yield includes usage and stop reason

### Azure Foundry Authentication

**Three authentication methods supported**:
1. **API Key**: `AnthropicFoundryApiKeyCredentials` - x-api-key header
2. **Bearer Token**: `AnthropicFoundryBearerTokenCredentials` - Authorization header
3. **Azure Identity**: `AnthropicFoundryIdentityTokenCredentials` - DefaultAzureCredential, Managed Identity

**Environment-based initialization**:
- `ANTHROPIC_FOUNDRY_RESOURCE` - Azure resource name (required)
- `ANTHROPIC_FOUNDRY_API_KEY` - API key (optional, uses Azure Identity if not set)

### Tool/Function Calling

**Mapping strategy**:
- `AIFunctionDeclaration` → Anthropic `ToolDefinition` with JSON schema
- `FunctionCallContent` → `ToolUseBlock` (type: "tool_use", id, name, input)
- `FunctionResultContent` → `ToolResultBlock` (type: "tool_result", tool_use_id, content)

### Multi-Modal Content

**Image handling**: Convert `DataContent` to base64 with MIME type → `ImageBlock`
**PDF handling**: Use Beta API, convert to base64 → `PDFBlock`
**Supported formats**: image/jpeg, image/png, image/gif, image/webp, application/pdf

## Target Framework

- **net9.0** - Latest .NET with C# 13 features

## Required Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `Anthropic.Foundry` | 0.0.1+ | Azure Anthropic Foundry client (PRIMARY) |
| `Anthropic` | 10.1.2+ | Standard Anthropic SDK (transitive) |
| `Azure.Identity` | 1.17.0+ | Azure authentication |
| `Microsoft.Rest.ClientRuntime.Azure.Authentication` | 2.4.1+ | Azure REST auth |
| `Microsoft.Extensions.AI.Abstractions` | Latest | Core abstractions |
| `System.Text.Json` | 9.0.9+ | JSON serialization |

## Development Commands

### Build
```bash
dotnet build
dotnet build -c Release
```

### Run Tests
```bash
# All tests
dotnet test

# Specific test project
dotnet test tests/Microsoft.Extensions.AI.Anthropic.Tests/

# Single test
dotnet test --filter "FullyQualifiedName~AnthropicChatClientTests.GetResponseAsync_BasicChat"

# With coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

### Run Examples
```bash
# Azure Foundry basic example (requires ANTHROPIC_FOUNDRY_RESOURCE env var)
dotnet run --project examples/AzureFoundryBasicExample/

# Standard API example (requires ANTHROPIC_API_KEY env var)
dotnet run --project examples/BasicChatExample/
```

### Package
```bash
dotnet pack -c Release -o ./artifacts
```

## Implementation Phases

Refer to `docs/research/anthropic-integration-research-plan.md` for the complete 4-phase implementation strategy:

1. **Phase 1**: Core Infrastructure - `AnthropicChatClient`, basic converters
2. **Phase 2**: Type Converters - Messages, content, options, tools
3. **Phase 3**: Streaming - Event aggregation and state machine
4. **Phase 4**: DI Extensions - Azure Foundry and standard API registration

## Quality Requirements

- **Test Coverage**: 90%+ target
- **Thread Safety**: All implementations must be thread-safe
- **Performance**: Streaming latency < 10ms per event
- **Security**: No API keys in code; Azure Identity for production

## Key Design Decisions

### Why Dual Client Support?
Single `AnthropicChatClient` accepts `IAnthropicClient` interface, supporting both `AnthropicClient` (standard) and `AnthropicFoundryClient` (Azure) without code duplication.

### Why Azure Foundry Primary?
Enterprise scenarios require Azure authentication (Managed Identity, RBAC), no hardcoded API keys, and Azure-native deployment patterns.

### Why Not Separate Packages?
Shared converters, minimal code differences, unified API surface, and easier maintenance justify single package with dual support.

## Reference Documentation

- **Research Plan**: `docs/research/anthropic-integration-research-plan.md` - Complete architecture analysis and implementation strategy
- **M.E.AI Docs**: https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai
- **Anthropic API Docs**: https://docs.anthropic.com/en/api
- **Anthropic C# SDK**: https://github.com/anthropics/anthropic-sdk-csharp
