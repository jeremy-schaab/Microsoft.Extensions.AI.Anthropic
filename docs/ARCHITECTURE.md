# Architecture Documentation - Microsoft.Extensions.AI.Anthropic

**Version**: 0.3.1-preview
**Last Updated**: 2025-01-19
**Audience**: .NET architects, senior developers, contributors

This document provides a comprehensive overview of the Microsoft.Extensions.AI.Anthropic library architecture, design decisions, and implementation patterns.

## Table of Contents

- [System Architecture](#system-architecture)
- [Component Overview](#component-overview)
- [Design Decisions](#design-decisions)
- [Type Conversion Layer](#type-conversion-layer)
- [Streaming Architecture](#streaming-architecture)
- [Authentication Flow](#authentication-flow)
- [Extension Points](#extension-points)
- [Performance Considerations](#performance-considerations)
- [Security Model](#security-model)

## System Architecture

### High-Level Overview

Microsoft.Extensions.AI.Anthropic implements the **Adapter Pattern** to bridge Anthropic's SDK with Microsoft.Extensions.AI abstractions.

```
┌─────────────────────────────────────────────────────────────────┐
│                     Application Layer                           │
│         (Uses Microsoft.Extensions.AI abstractions)             │
└───────────────────────┬─────────────────────────────────────────┘
                        │ IChatClient interface
┌───────────────────────▼─────────────────────────────────────────┐
│         Microsoft.Extensions.AI.Anthropic (This Library)        │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │ AnthropicChatClient (implements IChatClient)             │   │
│  │                                                          │   │
│  │  ┌────────────────────────────────────────────────────┐ │   │
│  │  │ Type Converters (Bidirectional)                    │ │   │
│  │  │  • AnthropicMessageConverter                       │ │   │
│  │  │  • AnthropicContentConverter                       │ │   │
│  │  │  • AnthropicOptionsConverter                       │ │   │
│  │  │  • AnthropicToolConverter                          │ │   │
│  │  │  • AnthropicStreamingConverter                     │ │   │
│  │  └────────────────────────────────────────────────────┘ │   │
│  │                                                          │   │
│  │  ┌────────────────────────────────────────────────────┐ │   │
│  │  │ Extension Methods (DI Integration)                 │ │   │
│  │  │  • IServiceCollection extensions                   │ │   │
│  │  │  • IChatClientBuilder extensions                   │ │   │
│  │  └────────────────────────────────────────────────────┘ │   │
│  └──────────────────────────────────────────────────────────┘   │
└───────────────────────┬─────────────────────────────────────────┘
                        │ IAnthropicClient / IMessageService
        ┌───────────────┴──────────────────┐
        │                                   │
┌───────▼────────────┐           ┌──────────▼──────────────┐
│  Anthropic SDK     │           │ Anthropic.Foundry SDK   │
│  (Embedded)        │           │ (Embedded)              │
│  - AnthropicClient │           │ - AnthropicFoundryClient│
│  - IMessageService │           │ - IMessageService       │
└───────┬────────────┘           └──────────┬──────────────┘
        │                                   │
┌───────▼────────────┐           ┌──────────▼──────────────┐
│ api.anthropic.com  │           │ *.ai.azure.com          │
│ (Standard API)     │           │ (Azure Foundry)         │
└────────────────────┘           └─────────────────────────┘
```

### Key Architectural Principles

1. **Single Responsibility**: Each converter handles one type mapping
2. **Open/Closed**: Extensible via middleware pattern
3. **Dependency Inversion**: Depends on abstractions (IChatClient, IAnthropicClient)
4. **Interface Segregation**: Minimal, focused interfaces
5. **Liskov Substitution**: AnthropicChatClient is fully substitutable for IChatClient

## Component Overview

### Core Components

#### 1. AnthropicChatClient

**Location**: `src/Microsoft.Extensions.AI.Anthropic/AnthropicChatClient.cs`

**Purpose**: Primary `IChatClient` implementation that orchestrates all operations.

**Responsibilities**:
- Accept requests via `GetResponseAsync` and `GetStreamingResponseAsync`
- Coordinate type conversions using converter components
- Invoke Anthropic SDK clients
- Expose metadata via `ChatClientMetadata`
- Implement service locator pattern via `GetService`

**Key Methods**:

```csharp
// Non-streaming chat
public Task<ChatResponse> GetResponseAsync(
    IEnumerable<ChatMessage> chatMessages,
    ChatOptions? options = null,
    CancellationToken cancellationToken = default)

// Streaming chat
public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
    IEnumerable<ChatMessage> chatMessages,
    ChatOptions? options = null,
    CancellationToken cancellationToken = default)
```

**Design Pattern**: Facade pattern - Simplifies complex interactions with Anthropic SDK.

#### 2. Type Converters

**Location**: `src/Microsoft.Extensions.AI.Anthropic/Converters/`

Five specialized converters handle bidirectional type mapping:

##### AnthropicMessageConverter

**Purpose**: Convert between `ChatMessage` (M.E.AI) and `MessageParam` (Anthropic).

**Key Responsibilities**:
- Extract system messages from message array
- Combine multiple system messages into single string
- Map roles: User, Assistant, Tool
- Handle message content conversion

**Algorithm**:
```
1. Separate system messages from conversation messages
2. Combine system message texts with double newline
3. Convert each non-system message:
   - Map ChatRole to Anthropic role
   - Convert AIContent[] to ContentBlock[]
4. Validate alternating user/assistant pattern
5. Return (messages, systemPrompt)
```

**Edge Cases Handled**:
- Empty message lists
- Multiple system messages (combined)
- Mixed content types in single message
- Tool result messages

##### AnthropicContentConverter

**Purpose**: Convert between `AIContent` (M.E.AI) and `ContentBlock` (Anthropic).

**Supported Conversions**:

| M.E.AI Type | Anthropic Type | Direction |
|-------------|----------------|-----------|
| `TextContent` | `TextBlock` | Bidirectional |
| `DataContent` (image) | `ImageBlock` | Bidirectional |
| `DataContent` (PDF) | `PDFBlock` | Request only |
| `FunctionCallContent` | `ToolUseBlock` | Bidirectional |
| `FunctionResultContent` | `ToolResultBlock` | Request only |

**Image Handling**:
```csharp
// Request: DataContent → ImageBlock
{
    type: "image",
    source: {
        type: "base64",
        media_type: "image/png",
        data: "iVBORw0KGgoAAAANSUhEUg..."
    }
}

// Response: ImageBlock → DataContent
// Note: Anthropic responses only include text and tool_use blocks
```

**Tool Call Handling**:
```csharp
// Request: FunctionCallContent → ToolUseBlock
{
    type: "tool_use",
    id: "call_abc123",
    name: "get_weather",
    input: { "location": "San Francisco" }
}

// Request: FunctionResultContent → ToolResultBlock
{
    type: "tool_result",
    tool_use_id: "call_abc123",
    content: "Sunny, 72°F"
}
```

##### AnthropicOptionsConverter

**Purpose**: Convert `ChatOptions` to Anthropic's `MessageCreateParams`.

**Mapping Table**:

| ChatOptions Property | MessageCreateParams Property | Transformation |
|---------------------|------------------------------|----------------|
| `ModelId` | `model` | Direct |
| `Temperature` | `temperature` | Direct (0.0-1.0) |
| `TopP` | `top_p` | Direct (0.0-1.0) |
| `TopK` | `top_k` | Direct (integer) |
| `MaxOutputTokens` | `max_tokens` | Direct (integer) |
| `StopSequences` | `stop_sequences` | Direct (string[]) |
| `Tools` | `tools` | Via AnthropicToolConverter |
| `ToolMode` | `tool_choice` | Map to Anthropic format |
| `AdditionalProperties["thinking"]` | `thinking` | Extended thinking (Opus 4) |

**Tool Choice Mapping**:
```csharp
// Auto (default)
AutoChatToolMode → { type: "auto" }

// Require any tool
RequireAnyChatToolMode → { type: "any" }

// Require specific tool
RequireSpecificChatToolMode → { type: "tool", name: "tool_name" }
```

##### AnthropicToolConverter

**Purpose**: Convert `AIFunctionDeclaration` to Anthropic `ToolDefinition` with JSON schema.

**Schema Generation Algorithm**:
```
1. Extract function metadata (name, description)
2. Parse function parameters using reflection
3. Infer JSON schema types:
   - string → "string"
   - int, long → "integer"
   - float, double → "number"
   - bool → "boolean"
   - T[] → "array" with items schema
   - object → "object" with properties schema
   - enum → "string" with enum constraint
4. Identify required parameters (non-nullable, no default)
5. Build JSON schema:
   {
     type: "object",
     properties: { ... },
     required: [ ... ]
   }
6. Return ToolDefinition
```

**Example**:
```csharp
// Input: AIFunctionDeclaration
var func = AIFunctionFactory.Create(
    (string location, string? units = "fahrenheit") => GetWeather(location, units),
    name: "get_weather",
    description: "Get weather for a location");

// Output: ToolDefinition
{
    name: "get_weather",
    description: "Get weather for a location",
    input_schema: {
        type: "object",
        properties: {
            location: { type: "string" },
            units: { type: "string" }
        },
        required: ["location"]
    }
}
```

##### AnthropicStreamingConverter

**Purpose**: Convert Anthropic streaming events to `ChatResponseUpdate` objects.

**State Machine**:
```
┌─────────────┐
│   Initial   │
└─────┬───────┘
      │ message_start
      ▼
┌─────────────┐
│  Streaming  │◄─────────────┐
└─────┬───────┘              │
      │ content_block_start  │
      ▼                      │
┌─────────────┐              │
│   Content   │──────────────┘
│   Block     │ content_block_delta
└─────┬───────┘
      │ content_block_stop
      ▼
┌─────────────┐
│   Next      │──────────────┐
│   Block     │              │
└─────┬───────┘              │
      │ content_block_start  │
      └──────────────────────┘
      │ message_delta
      ▼
┌─────────────┐
│   Usage     │
└─────┬───────┘
      │ message_stop
      ▼
┌─────────────┐
│   Complete  │
└─────────────┘
```

**Event Handling**:

| Event Type | Action | Yields Update |
|------------|--------|---------------|
| `message_start` | Initialize state, set message ID | No |
| `content_block_start` | Begin new content accumulator | No |
| `content_block_delta` | Append text delta, accumulate tool input | Yes (text delta) |
| `content_block_stop` | Finalize content block | Yes (tool calls) |
| `message_delta` | Update stop reason | Yes (finish reason) |
| `message_stop` | Finalize message | Yes (usage) |

**Optimization**: Uses `StringBuilder` for text accumulation to avoid string concatenation overhead.

### Extension Methods

**Location**: `src/Microsoft.Extensions.AI.Anthropic/Extensions/`

#### IServiceCollection Extensions

Enable dependency injection registration:

```csharp
// Azure Foundry
services.AddAnthropicFoundryChatClientFromEnvironment(modelId: "claude-sonnet-4-5");
services.AddAnthropicFoundryChatClient(resourceName, apiKey, modelId);
services.AddAnthropicFoundryChatClient(credentials, modelId);
services.AddAnthropicFoundryChatClient(clientFactory, modelId);

// Standard API
services.AddAnthropicChatClient(apiKey, modelId);
services.AddAnthropicChatClient(clientFactory, modelId);
```

#### IChatClientBuilder Extensions

Enable middleware pipeline configuration:

```csharp
services.AddChatClient(builder => builder
    .UseAnthropicFoundryFromEnvironment(modelId: "claude-sonnet-4-5")
    .UseLogging()
    .UseOpenTelemetry()
    .UseRetryPolicy());
```

**Middleware Pipeline**:
```
Request
  │
  ▼
[Logging Middleware]
  │
  ▼
[OpenTelemetry Middleware]
  │
  ▼
[Retry Middleware]
  │
  ▼
[AnthropicChatClient]
  │
  ▼
Anthropic API
```

## Design Decisions

### 1. Why Dual Client Support?

**Decision**: Single `AnthropicChatClient` supports both `AnthropicClient` (standard API) and `AnthropicFoundryClient` (Azure Foundry).

**Rationale**:
- **Code Reuse**: Converters work for both clients
- **Unified API**: Developers use same `IChatClient` interface
- **Simpler Deployment**: One package, two APIs
- **Maintenance**: Single codebase to maintain

**Alternative Considered**: Separate packages (`Microsoft.Extensions.AI.Anthropic` and `Microsoft.Extensions.AI.Anthropic.Foundry`)
- **Rejected**: Too much duplication, harder to maintain

### 2. Why Embed Anthropic SDKs?

**Decision**: Include `Anthropic` and `Anthropic.Foundry` SDKs as embedded dependencies.

**Rationale**:
- **Single DLL**: Easier deployment (one package, no transitive dependencies)
- **Version Control**: Ensure compatible SDK versions
- **No Conflicts**: Avoid version conflicts with app dependencies
- **Simpler NuGet**: Users install one package, not three

**Implementation**:
```xml
<ItemGroup>
  <ProjectReference Include="..\Anthropic.Foundry\Anthropic.Foundry.csproj" PrivateAssets="all" />
  <ProjectReference Include="..\Anthropic\Anthropic.csproj" PrivateAssets="all" />
</ItemGroup>

<Target Name="CopyProjectReferencesToPackage" DependsOnTargets="ResolveReferences">
  <ItemGroup>
    <BuildOutputInPackage Include="@(ReferenceCopyLocalPaths->WithMetadataValue('ReferenceSourceTarget', 'ProjectReference'))" />
  </ItemGroup>
</Target>
```

### 3. Why System Message Extraction?

**Decision**: Extract system messages from message array and send via separate `system` parameter.

**Anthropic API Requirement**:
```json
// Anthropic API format
{
  "model": "claude-sonnet-4-5",
  "system": "You are a helpful assistant.",
  "messages": [
    { "role": "user", "content": "Hello" },
    { "role": "assistant", "content": "Hi!" }
  ]
}
```

**Microsoft.Extensions.AI Format**:
```csharp
var messages = new List<ChatMessage>
{
    new(ChatRole.System, "You are a helpful assistant."),
    new(ChatRole.User, "Hello"),
    new(ChatRole.Assistant, "Hi!")
};
```

**Conversion Process**:
1. Identify all `ChatRole.System` messages
2. Extract their text content
3. Combine with `\n\n` separator
4. Set `MessageCreateParams.System` parameter
5. Exclude system messages from `messages` array

**Multiple System Messages**:
```csharp
// Input
var messages = new[]
{
    new ChatMessage(ChatRole.System, "You are a Python expert."),
    new ChatMessage(ChatRole.System, "Always provide code examples."),
    new ChatMessage(ChatRole.User, "How do I read a file?")
};

// Output system parameter
"You are a Python expert.\n\nAlways provide code examples."
```

### 4. Why Streaming State Machine?

**Decision**: Implement stateful event aggregation for streaming responses.

**Anthropic Streaming Events** (fine-grained):
```
message_start         → { id, model, role }
content_block_start   → { index, type: "text" }
content_block_delta   → { index, delta: { type: "text_delta", text: "Hello" } }
content_block_delta   → { index, delta: { type: "text_delta", text: " world" } }
content_block_stop    → { index }
message_delta         → { stop_reason: "end_turn", usage: {...} }
message_stop          → { }
```

**Microsoft.Extensions.AI Format** (message-level):
```csharp
ChatResponseUpdate
{
    Text = "Hello",
    Contents = [new TextContent("Hello")],
    // ...
}

ChatResponseUpdate
{
    Text = " world",
    Contents = [new TextContent(" world")],
    // ...
}

ChatResponseUpdate
{
    FinishReason = ChatFinishReason.Stop,
    Usage = new UsageContent(...),
    // ...
}
```

**State Machine Benefits**:
- Accumulates text deltas across multiple events
- Handles tool call streaming (JSON accumulation)
- Yields meaningful updates (not internal bookkeeping events)
- Provides complete usage information in final update

## Type Conversion Layer

### Bidirectional Conversion Flow

```
┌──────────────────────────────────────────────────────────────┐
│                      Application                             │
└────────────┬─────────────────────────────────────────────────┘
             │ ChatMessage, ChatOptions
             ▼
┌──────────────────────────────────────────────────────────────┐
│               AnthropicMessageConverter                      │
│               AnthropicOptionsConverter                      │
│               AnthropicContentConverter                      │
│               AnthropicToolConverter                         │
└────────────┬─────────────────────────────────────────────────┘
             │ MessageParam[], MessageCreateParams
             ▼
┌──────────────────────────────────────────────────────────────┐
│                   Anthropic SDK                              │
│              AnthropicClient / AnthropicFoundryClient        │
└────────────┬─────────────────────────────────────────────────┘
             │ HTTP Request (JSON)
             ▼
┌──────────────────────────────────────────────────────────────┐
│                   Anthropic API                              │
└────────────┬─────────────────────────────────────────────────┘
             │ HTTP Response (JSON)
             ▼
┌──────────────────────────────────────────────────────────────┐
│                   Anthropic SDK                              │
│              Message (response object)                       │
└────────────┬─────────────────────────────────────────────────┘
             │ Message object
             ▼
┌──────────────────────────────────────────────────────────────┐
│               AnthropicMessageConverter                      │
│               AnthropicContentConverter                      │
└────────────┬─────────────────────────────────────────────────┘
             │ ChatResponse
             ▼
┌──────────────────────────────────────────────────────────────┐
│                      Application                             │
└──────────────────────────────────────────────────────────────┘
```

### Type Mapping Reference

**Messages**:
| M.E.AI | Anthropic | Notes |
|--------|-----------|-------|
| `ChatMessage` | `MessageParam` | Includes role and content |
| `ChatRole.System` | `system` parameter | Extracted from messages |
| `ChatRole.User` | `"user"` | Direct mapping |
| `ChatRole.Assistant` | `"assistant"` | Direct mapping |
| `ChatRole.Tool` | `"user"` with tool_result | Tool results as user messages |

**Content**:
| M.E.AI | Anthropic | MIME Type |
|--------|-----------|-----------|
| `TextContent` | `TextBlock` | N/A |
| `DataContent` | `ImageBlock` | image/jpeg, image/png, image/gif, image/webp |
| `DataContent` | `PDFBlock` | application/pdf (Beta, Opus 4 only) |
| `FunctionCallContent` | `ToolUseBlock` | N/A |
| `FunctionResultContent` | `ToolResultBlock` | N/A |

**Options**:
| M.E.AI | Anthropic | Range |
|--------|-----------|-------|
| `Temperature` | `temperature` | 0.0 - 1.0 |
| `TopP` | `top_p` | 0.0 - 1.0 |
| `TopK` | `top_k` | Integer > 0 |
| `MaxOutputTokens` | `max_tokens` | Integer > 0 |
| `StopSequences` | `stop_sequences` | String[] (max 4) |

## Streaming Architecture

### Event Flow Diagram

```
Anthropic API (SSE)
  │
  ▼
┌──────────────────────────────┐
│ Raw SSE Events (JSON strings)│
└──────────┬───────────────────┘
           │
           ▼
┌──────────────────────────────┐
│  Anthropic SDK Parser        │
│  (IAsyncEnumerable<Event>)   │
└──────────┬───────────────────┘
           │
           ▼
┌──────────────────────────────┐
│ AnthropicStreamingConverter  │
│  - State Machine             │
│  - Text Accumulation         │
│  - Tool Call Aggregation     │
└──────────┬───────────────────┘
           │
           ▼
┌──────────────────────────────┐
│ ChatResponseUpdate Stream    │
│ (IAsyncEnumerable<Update>)   │
└──────────┬───────────────────┘
           │
           ▼
       Application
```

### Performance Optimizations

1. **StringBuilder for Text**: Avoids string concatenation overhead
2. **Lazy Yielding**: Only yields when meaningful content accumulated
3. **Incremental Updates**: Streams text as it arrives (no buffering)
4. **Memory Efficient**: Disposes state after completion

## Authentication Flow

### Azure Foundry with DefaultAzureCredential

```
Application Startup
  │
  ▼
DefaultAzureCredential.GetTokenAsync()
  │
  ▼
┌──────────────────────────────┐
│ Credential Chain (in order): │
│ 1. EnvironmentCredential     │◄── AZURE_CLIENT_ID, AZURE_CLIENT_SECRET, AZURE_TENANT_ID
│ 2. ManagedIdentityCredential │◄── Azure App Service, Functions, VM
│ 3. VisualStudioCredential    │◄── Visual Studio login
│ 4. AzureCliCredential        │◄── az login
│ 5. ...                       │
└──────────┬───────────────────┘
           │ Token acquired
           ▼
AnthropicFoundryIdentityTokenCredentials
  │
  ▼
AnthropicFoundryClient
  │ Authorization: Bearer {token}
  ▼
Azure Foundry API
```

## Extension Points

### 1. Middleware Pipeline

Add custom middleware by implementing `IChatClient`:

```csharp
public class RateLimitingChatClient : IChatClient
{
    private readonly IChatClient _innerClient;
    private readonly SemaphoreSlim _semaphore;

    public RateLimitingChatClient(IChatClient innerClient, int maxConcurrency)
    {
        _innerClient = innerClient;
        _semaphore = new SemaphoreSlim(maxConcurrency);
    }

    public async Task<ChatResponse> GetResponseAsync(...)
    {
        await _semaphore.WaitAsync();
        try
        {
            return await _innerClient.GetResponseAsync(...);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}

// Usage
builder.Services.AddChatClient(b => b
    .UseAnthropicFoundryFromEnvironment()
    .Use(client => new RateLimitingChatClient(client, maxConcurrency: 5)));
```

### 2. Custom Converters

Extend conversion logic by wrapping converters:

```csharp
public static class CustomContentConverter
{
    public static List<ContentBlock> ToAnthropicContent(AIContent content)
    {
        // Custom conversion logic
        // Fallback to AnthropicContentConverter.ToAnthropicContent(content)
    }
}
```

### 3. Custom Credentials

Implement custom token providers:

```csharp
public class CustomTokenCredential : TokenCredential
{
    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        // Custom token acquisition logic
        return new AccessToken("custom-token", DateTimeOffset.UtcNow.AddHours(1));
    }

    public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        return new ValueTask<AccessToken>(GetToken(requestContext, cancellationToken));
    }
}
```

## Performance Considerations

### Streaming vs Non-Streaming

**Non-Streaming** (GetResponseAsync):
- **Latency**: Higher (waits for complete response)
- **Memory**: Lower (single response object)
- **Use Case**: Batch processing, short responses

**Streaming** (GetStreamingResponseAsync):
- **Latency**: Lower (first token TTFT ~300ms)
- **Memory**: Higher (maintains state machine)
- **Use Case**: Interactive chat, long responses, real-time UX

### Memory Management

**Text Accumulation**:
- Uses `StringBuilder` (amortized O(1) append)
- Max practical message size: ~100K tokens (~400K chars)

**Tool Call Aggregation**:
- Accumulates JSON strings for tool inputs
- Parsed once per tool call (not per delta)

### Concurrency

**Thread Safety**:
- `AnthropicChatClient` is thread-safe for read operations
- Converters are stateless (thread-safe)
- Streaming state is per-request (isolated)

**Best Practices**:
- Reuse `AnthropicChatClient` instance (registered as singleton in DI)
- Limit concurrent requests (use semaphore if needed)
- Use streaming for long responses

## Security Model

### Credential Hierarchy

**Production** (Most Secure → Least Secure):
1. **Managed Identity** (Azure resources)
   - No secrets in code or configuration
   - Automatic rotation
   - Azure RBAC integration
2. **Azure Key Vault Reference** (App Settings)
   - Secrets stored securely
   - Access logged
   - Rotation support
3. **Service Principal** (CI/CD)
   - Scoped permissions
   - Auditable
   - Manual rotation required
4. **API Key** (Development only)
   - Simple but less secure
   - Manual rotation required
   - Risk of exposure

### Secrets Management

**Never**:
- Hardcode API keys in source code
- Commit secrets to source control
- Log API keys or tokens
- Store secrets in plain text configuration

**Always**:
- Use environment variables
- Use Azure Key Vault for production
- Rotate keys regularly (90 days)
- Monitor access logs
- Use Managed Identity when possible

---

**Related Documentation**:
- [Getting Started Guide](GETTING-STARTED.md)
- [API Reference](API-REFERENCE.md)
- [Authentication Guide](AUTHENTICATION-GUIDE.md)
- [Examples Guide](EXAMPLES-GUIDE.md)
