# Research Plan: Microsoft.Extensions.AI.Anthropic Implementation

**Date:** 2025-11-19
**Updated:** 2025-11-19
**Objective:** Create a Microsoft.Extensions.AI.Anthropic project that integrates Anthropic's Claude AI models (via Azure Anthropic Foundry) with the Microsoft.Extensions.AI abstractions framework.

---

## Executive Summary

This research plan outlines the strategy for building a new `Microsoft.Extensions.AI.Anthropic` library that enables .NET developers to use Anthropic's Claude models through the standardized `Microsoft.Extensions.AI` abstractions. The implementation will follow the established pattern used by `Microsoft.Extensions.AI.OpenAI` as a reference architecture.

### 🎯 PRIMARY TARGET: Azure Anthropic Foundry

**CRITICAL: This implementation will primarily target `AnthropicFoundryClient`** for Azure-hosted Anthropic API, with secondary support for standard `AnthropicClient`.

**AnthropicFoundryClient Key Features:**
- **Azure-hosted Anthropic API** at `https://{resourceName}.services.ai.azure.com/anthropic`
- **Azure Authentication** via `Azure.Identity` (DefaultAzureCredential, Managed Identity, etc.)
- **Multiple credential types**: API Key (`x-api-key`), Bearer Token, Azure Identity
- **Package dependency**: `Anthropic.Foundry` (in addition to `Anthropic`)
- Inherits from `AnthropicClient` but overrides authentication mechanism

### Key Deliverables
1. `Microsoft.Extensions.AI.Anthropic` NuGet package
2. Implementation of `IChatClient` interface wrapping **AnthropicFoundryClient** (primary) and AnthropicClient (secondary)
3. Azure authentication integration (Azure.Identity support)
4. Type converters between M.E.AI and Anthropic models
5. Dependency injection extensions with Azure credential support
6. Comprehensive test suite (including Azure-hosted scenarios)
7. Documentation and examples for both Azure Foundry and standard Anthropic

---

## 1. Architecture Analysis

### 1.1 Microsoft.Extensions.AI.Abstractions Overview

**Core Abstractions Identified:**

| Interface/Type | Purpose | Location |
|---|---|---|
| `IChatClient` | Main abstraction for chat completion | IChatClient.cs:33 |
| `ChatMessage` | Message container with role and content | ChatMessage.cs:15 |
| `ChatOptions` | Configuration for chat requests | ChatOptions.cs:13 |
| `ChatResponse` | Non-streaming response wrapper | - |
| `ChatResponseUpdate` | Streaming response chunk | - |
| `AIContent` | Base class for content types | - |
| `ChatRole` | Enum: System, User, Assistant, Tool | - |

**Key Content Types:**
- `TextContent` - Plain text messages
- `DataContent` - Binary data (images, audio, PDFs) with MIME types
- `UriContent` - Content via URI reference
- `FunctionCallContent` - Tool/function invocations from model
- `FunctionResultContent` - Tool/function results to model
- `UsageContent` - Token usage tracking
- `ErrorContent` - Error information

**Tool/Function Support:**
- `AIFunctionDeclaration` - Function definition
- `AITool` - Tool abstraction
- `ChatToolMode` - Base for tool modes (Auto, Required, None)

### 1.2 Microsoft.Extensions.AI.OpenAI Reference Implementation

**Project Structure Analysis:**

```
Microsoft.Extensions.AI.OpenAI/
├── OpenAIChatClient.cs                          (Main IChatClient implementation)
├── OpenAIAssistantsChatClient.cs               (Assistants API wrapper)
├── OpenAIResponsesChatClient.cs                (Special responses wrapper)
├── MicrosoftExtensionsAIChatExtensions.cs      (DI/Builder extensions)
└── Microsoft.Extensions.AI.OpenAI.csproj       (Project file)
```

**Key Dependencies (from .csproj):**
- `OpenAI` package (Azure.AI.OpenAI SDK)
- `System.Memory.Data`
- `System.Text.Json`
- `Microsoft.Extensions.AI.Abstractions` (project reference)

**Implementation Pattern Analysis (OpenAIChatClient.cs:27-794):**

1. **Constructor Pattern:**
   - Takes vendor SDK client (`ChatClient`) as parameter
   - Stores metadata (provider, endpoint, model ID)
   - Lines 56-64

2. **GetResponseAsync Pattern (Lines 80-95):**
   ```
   Input: IEnumerable<ChatMessage>, ChatOptions
   ↓
   Convert to vendor types → ToOpenAIChatMessages(), ToOpenAIOptions()
   ↓
   Call vendor SDK → _chatClient.CompleteChatAsync()
   ↓
   Convert response → FromOpenAIChatCompletion()
   ↓
   Output: ChatResponse
   ```

3. **GetStreamingResponseAsync Pattern (Lines 98-112):**
   ```
   Input: IEnumerable<ChatMessage>, ChatOptions
   ↓
   Convert to vendor types
   ↓
   Call streaming API → _chatClient.CompleteChatStreamingAsync()
   ↓
   Convert stream with IAsyncEnumerable → FromOpenAIStreamingChatCompletionAsync()
   ↓
   Output: IAsyncEnumerable<ChatResponseUpdate>
   ```

4. **Key Conversion Methods:**
   - `ToOpenAIChatMessages()` - Lines 135-241
   - `ToOpenAIOptions()` - Lines 553-621
   - `FromOpenAIChatCompletion()` - Lines 461-550
   - `FromOpenAIStreamingChatCompletionAsync()` - Lines 325-448

5. **Service Provider Pattern (Lines 67-77):**
   - Returns metadata, underlying client, or self
   - Enables service discovery and debugging

### 1.3 Anthropic C# SDK Architecture

#### 1.3.1 Standard AnthropicClient (Anthropic Package)

**Main Client Structure (AnthropicClient.cs:15-319):**
- Base client handles HTTP, retries, auth
- Service properties: `Messages`, `Models`, `Beta`
- Configuration via `ClientOptions`
- Supports API key and auth token authentication
- Base URL: `https://api.anthropic.com` (default)

**Message Service Interface (IMessageService.cs:16-62):**
```csharp
public interface IMessageService
{
    Task<Message> Create(MessageCreateParams, CancellationToken);
    IAsyncEnumerable<RawMessageStreamEvent> CreateStreaming(MessageCreateParams, CancellationToken);
    Task<MessageTokensCount> CountTokens(MessageCountTokensParams, CancellationToken);
}
```

**Key Request/Response Types:**
- `MessageCreateParams` - Request parameters (messages, model, max_tokens, etc.)
- `Message` - Response with content blocks
- `RawMessageStreamEvent` - Streaming event
- `TextBlock` - Text content block
- `ToolUseBlock` - Tool call content
- `Role` - Enum: User, Assistant

**Streaming Architecture:**
- Server-Sent Events (SSE) pattern
- Multiple event types: message_start, content_block_start, content_block_delta, message_delta, message_stop
- Returns `IAsyncEnumerable<RawMessageStreamEvent>`

#### 1.3.2 🎯 AnthropicFoundryClient (Anthropic.Foundry Package) - PRIMARY TARGET

**Client Structure (AnthropicFoundryClient.cs:13-44):**
- **Extends `AnthropicClient`** - Inherits all message API functionality
- **Azure-specific base URL**: `https://{resourceName}.services.ai.azure.com/anthropic`
- **Azure authentication** via `IAnthropicFoundryCredentials`
- **Credential types** (all implement `IAnthropicFoundryCredentials`):
  1. **`AnthropicFoundryApiKeyCredentials`** - API key via `x-api-key` header
  2. **`AnthropicFoundryBearerTokenCredentials`** - Bearer token authentication
  3. **`AnthropicFoundryIdentityTokenCredentials`** - Azure Identity (DefaultAzureCredential, Managed Identity, etc.)

**Authentication Flow (AnthropicFoundryClient.cs:38-43):**
```csharp
protected override ValueTask BeforeSend<T>(HttpRequest<T> request,
    HttpRequestMessage requestMessage, CancellationToken cancellationToken)
{
    _azureCredentials.Apply(requestMessage);  // Applies auth header
    return ValueTask.CompletedTask;
}
```

**Credential Factory (IAnthropicFoundryCredentials.cs:36-61):**
```csharp
// Environment-based initialization
var credentials = await IAnthropicFoundryCredentials.FromEnv();
// Looks for:
// - ANTHROPIC_FOUNDRY_RESOURCE (required)
// - ANTHROPIC_FOUNDRY_API_KEY (optional - if set, uses API key auth)
// - If no API key, uses DefaultAzureCredential for Azure Identity
```

**Example Usage:**
```csharp
// Option 1: API Key authentication
var credentials = new AnthropicFoundryApiKeyCredentials(
    apiKey: "your-api-key",
    resourceName: "your-azure-resource"
);
var client = new AnthropicFoundryClient(credentials);

// Option 2: Azure Identity (DefaultAzureCredential)
var credentials = await IAnthropicFoundryCredentials.FromEnv();
var client = new AnthropicFoundryClient(credentials);

// Option 3: Bearer token
var credentials = new AnthropicFoundryBearerTokenCredentials(
    apiKey: "your-bearer-token",
    resourceName: "your-azure-resource"
);
var client = new AnthropicFoundryClient(credentials);

// Usage is identical to AnthropicClient
var response = await client.Messages.Create(parameters);
```

**Package Dependencies (Anthropic.Foundry.csproj:16-19):**
- `Anthropic` (core SDK)
- `Microsoft.Rest.ClientRuntime.Azure.Authentication` v2.4.1
- `Azure.Identity` v1.17.0

---

## 2. Type Mapping Strategy

### 2.1 Core Type Conversions

| M.E.AI Type | Anthropic SDK Type | Mapping Complexity |
|---|---|---|
| `ChatMessage` | `MessageParam` | **MEDIUM** - Role mapping, content arrays |
| `ChatRole.User` | `Role.User` | **SIMPLE** - Direct enum mapping |
| `ChatRole.Assistant` | `Role.Assistant` | **SIMPLE** - Direct enum mapping |
| `ChatRole.System` | System message in params | **COMPLEX** - Separate `system` param in Anthropic |
| `ChatRole.Tool` | Tool result block | **MEDIUM** - Tool result content block |
| `TextContent` | `TextBlock` | **SIMPLE** - Direct text mapping |
| `DataContent` (image) | `ImageBlock` with base64 | **MEDIUM** - Base64 encoding + MIME type |
| `DataContent` (PDF) | `PDFBlock` with base64 | **MEDIUM** - PDF support via beta |
| `FunctionCallContent` | `ToolUseBlock` | **COMPLEX** - Tool use structure |
| `FunctionResultContent` | `ToolResultBlock` | **COMPLEX** - Tool result structure |
| `ChatOptions` | `MessageCreateParams` | **COMPLEX** - Multiple field mappings |
| `ChatResponse` | `Message` | **MEDIUM** - Content extraction |
| `ChatResponseUpdate` | `RawMessageStreamEvent` | **COMPLEX** - Event aggregation |
| `AIFunctionDeclaration` | `ToolDefinition` | **MEDIUM** - Schema mapping |
| `UsageContent` | Token usage from Message | **SIMPLE** - Token counts |

### 2.2 Special Considerations

**System Messages:**
- M.E.AI: System messages are part of the message array
- Anthropic: System prompt is a separate parameter in `MessageCreateParams.System`
- **Strategy:** Extract system messages and combine into system parameter

**Tool Calling:**
- M.E.AI: `FunctionCallContent` and `FunctionResultContent` in message contents
- Anthropic: `ToolUseBlock` and `ToolResultBlock` in content arrays
- **Strategy:** Map between content block types, handle tool definitions

**Streaming Events:**
- Anthropic emits granular events: `message_start`, `content_block_start`, `content_block_delta`, `content_block_delta`, `message_delta`, `message_stop`
- M.E.AI expects: `ChatResponseUpdate` objects with accumulated content
- **Strategy:** Implement event aggregation and state machine for streaming

**Extended Thinking (Claude Opus 4 feature):**
- Anthropic: `thinking` content blocks with reasoning traces
- M.E.AI: `TextReasoningContent` for reasoning
- **Strategy:** Map thinking blocks to reasoning content

**Content Block Ordering:**
- Anthropic supports multiple content blocks per message
- M.E.AI uses `IList<AIContent>`
- **Strategy:** Preserve ordering, map each block type

---

## 3. Implementation Plan

### Phase 1: Core Infrastructure (Week 1)

**3.1 Project Setup**
- [ ] Create `Microsoft.Extensions.AI.Anthropic.csproj`
  - Target frameworks: net8.0, net9.0, netstandard2.0 (match abstractions)
  - **Package references**:
    - **`Anthropic.Foundry`** (includes `Anthropic` as transitive dependency) - PRIMARY
    - `Anthropic` (for standard API support) - SECONDARY
    - `Azure.Identity` v1.17.0+ (Azure authentication)
    - `Microsoft.Rest.ClientRuntime.Azure.Authentication` v2.4.1+ (Azure auth support)
    - `System.Text.Json`
    - `Microsoft.Extensions.AI.Abstractions` (project reference)
- [ ] Setup solution structure
- [ ] Configure build properties (nullable, LangVersion, etc.)

**3.2 Core Client Implementation**
- [ ] Create `AnthropicChatClient.cs` implementing `IChatClient`
  - **Constructor accepting `IAnthropicClient`** (supports both `AnthropicClient` and `AnthropicFoundryClient`)
  - Alternative constructor accepting `IMessageService` directly
  - Implement `GetResponseAsync()` - non-streaming
  - Implement `GetStreamingResponseAsync()` - streaming
  - Implement `GetService()` - service provider pattern
  - Implement `Dispose()` - cleanup
  - Store metadata indicating whether using Azure Foundry or standard API
- [ ] Create `ChatClientMetadata` instance with provider info
  - Provider name: "anthropic" or "anthropic-foundry"
  - Endpoint: Azure URL or standard API URL
  - Model ID from client

### Phase 2: Type Converters (Week 1-2)

**3.3 Message Converters**
- [ ] Create `AnthropicMessageConverter.cs`
  - `ToAnthropicMessages()` - Convert `IEnumerable<ChatMessage>` to message array
  - Handle system message extraction
  - `FromAnthropicMessage()` - Convert `Message` to `ChatMessage`
  - Handle role mapping

**3.4 Content Converters**
- [ ] Create `AnthropicContentConverter.cs`
  - `ToAnthropicContentBlocks()` - Convert `AIContent` to content blocks
    - Text → TextBlock
    - DataContent (image) → ImageBlock
    - DataContent (PDF) → PDFBlock
    - FunctionCallContent → ToolUseBlock
    - FunctionResultContent → ToolResultBlock
  - `FromAnthropicContentBlock()` - Reverse conversion
    - TextBlock → TextContent
    - ImageBlock → DataContent
    - ToolUseBlock → FunctionCallContent
    - Thinking blocks → TextReasoningContent (if supported)

**3.5 Options Converter**
- [ ] Create `AnthropicOptionsConverter.cs`
  - `ToAnthropicMessageCreateParams()` - Convert `ChatOptions` to request params
    - Map Temperature, MaxOutputTokens, TopP, TopK
    - Map StopSequences
    - Map Tools and ToolMode
    - Handle ModelId override
    - Extract and set system message
  - Handle `RawRepresentationFactory` for advanced scenarios

**3.6 Tool/Function Converters**
- [ ] Create `AnthropicToolConverter.cs`
  - `ToAnthropicToolDefinition()` - Convert `AIFunctionDeclaration` to tool schema
  - Map JSON schema from function parameters
  - Handle tool choice modes (auto, required, specific function)

### Phase 3: Streaming Implementation (Week 2)

**3.7 Streaming Event Handler**
- [ ] Create `AnthropicStreamingConverter.cs`
  - `FromAnthropicStreamAsync()` - Convert `IAsyncEnumerable<RawMessageStreamEvent>` to `ChatResponseUpdate`
  - Implement state machine for event types:
    - `message_start` - Initialize response
    - `content_block_start` - Start content accumulation
    - `content_block_delta` - Accumulate text/tool deltas
    - `message_delta` - Update stop reason, usage
    - `message_stop` - Finalize response
  - Handle tool call streaming (tool use deltas)
  - Aggregate partial content into complete `ChatResponseUpdate` objects

### Phase 4: Dependency Injection Extensions (Week 2)

**3.8 Builder Extensions**
- [ ] Create `MicrosoftExtensionsAIAnthropicExtensions.cs`

  **🎯 PRIMARY: Azure Foundry Extensions**
  - Extension methods for `IChatClientBuilder`:
    ```csharp
    // Azure Foundry with credentials object
    public static IChatClientBuilder AddAnthropicFoundryChatClient(
        this IChatClientBuilder builder,
        IAnthropicFoundryCredentials credentials,
        string? modelId = null)

    // Azure Foundry with resource name + API key
    public static IChatClientBuilder AddAnthropicFoundryChatClient(
        this IChatClientBuilder builder,
        string resourceName,
        string apiKey,
        string? modelId = null)

    // Azure Foundry from environment variables
    public static IChatClientBuilder AddAnthropicFoundryChatClientFromEnvironment(
        this IChatClientBuilder builder,
        string? resourceName = null,
        string? modelId = null)

    // Azure Foundry with explicit client
    public static IChatClientBuilder AddAnthropicFoundryChatClient(
        this IChatClientBuilder builder,
        AnthropicFoundryClient foundryClient)
    ```

  - Extension methods for `IServiceCollection`:
    ```csharp
    // Azure Foundry variants matching builder extensions
    public static IServiceCollection AddAnthropicFoundryChatClient(
        this IServiceCollection services,
        IAnthropicFoundryCredentials credentials,
        string? modelId = null)

    public static IServiceCollection AddAnthropicFoundryChatClientFromEnvironment(
        this IServiceCollection services,
        string? resourceName = null,
        string? modelId = null)
    ```

  **SECONDARY: Standard Anthropic Extensions**
  - Extension methods for standard API:
    ```csharp
    public static IChatClientBuilder AddAnthropicChatClient(
        this IChatClientBuilder builder,
        string apiKey,
        string? modelId = null)

    public static IChatClientBuilder AddAnthropicChatClient(
        this IChatClientBuilder builder,
        IAnthropicClient anthropicClient)
    ```

### Phase 5: Testing (Week 3)

**3.9 Unit Tests**
- [ ] Create `Microsoft.Extensions.AI.Anthropic.Tests.csproj`
- [ ] Test projects:
  - `AnthropicChatClientTests` - Core client functionality
  - `MessageConverterTests` - Message conversion
  - `ContentConverterTests` - Content block conversion
  - `OptionsConverterTests` - Options mapping
  - `ToolConverterTests` - Tool/function mapping
  - `StreamingTests` - Streaming event handling

**3.10 Integration Tests**
- [ ] Real API tests (with actual Anthropic API key)
  - Basic chat completion
  - Streaming responses
  - Tool calling scenarios
  - Multi-modal inputs (images, PDFs)
  - System message handling
  - Error scenarios

**3.11 Example Projects**
- [ ] Create `examples/BasicChatExample`
- [ ] Create `examples/StreamingChatExample`
- [ ] Create `examples/ToolCallingExample`
- [ ] Create `examples/VisionExample`

### Phase 6: Advanced Features (Week 3-4)

**3.12 Beta Features Support**
- [ ] Extended thinking support (Claude Opus 4)
  - Map thinking blocks to `TextReasoningContent`
  - Support `budget_tokens` parameter
- [ ] Prompt caching (if applicable)
- [ ] Batch API support (future consideration)

**3.13 Error Handling**
- [ ] Map Anthropic exceptions to standard exceptions
- [ ] Handle rate limiting with retry headers
- [ ] Handle content filtering errors
- [ ] Provide meaningful error messages

**3.14 Performance Optimizations**
- [ ] Minimize allocations in streaming path
- [ ] Efficient JSON serialization
- [ ] Connection pooling (via HttpClient)
- [ ] Memory-efficient base64 encoding for images

---

## 4. Technical Challenges & Solutions

### Challenge 1: System Message Handling
**Problem:** Anthropic uses a separate `system` parameter, M.E.AI includes system messages in the message array.

**Solution:**
1. In `ToAnthropicMessages()`, extract all `ChatRole.System` messages
2. Combine their text content into a single system prompt string
3. Set `MessageCreateParams.System` with combined text
4. Exclude system messages from the message array
5. In `FromAnthropicMessage()`, prepend system message to response if needed

### Challenge 2: Streaming Event Aggregation
**Problem:** Anthropic emits fine-grained events, M.E.AI expects message-level updates.

**Solution:**
1. Maintain state for current message being streamed
2. Accumulate text deltas into StringBuilder
3. Track tool calls across multiple content_block events
4. Yield `ChatResponseUpdate` when meaningful content is accumulated
5. Final yield with usage and stop reason from `message_stop` event

**State Machine:**
```
message_start → Initialize response metadata (id, model, role)
content_block_start → Begin new content block (type: text/tool_use)
content_block_delta → Accumulate text or tool arguments
content_block_stop → Finalize content block, yield update
message_delta → Update usage, stop reason
message_stop → Yield final update with usage
```

### Challenge 3: Tool Calling Differences
**Problem:** Different tool definition and result formats.

**Solution:**
1. Convert `AIFunctionDeclaration` to Anthropic tool schema:
   ```csharp
   {
       "name": function.Name,
       "description": function.Description,
       "input_schema": { /* JSON schema from parameters */ }
   }
   ```
2. Map `FunctionCallContent` to `ToolUseBlock`:
   ```csharp
   {
       "type": "tool_use",
       "id": callContent.CallId,
       "name": callContent.Name,
       "input": callContent.Arguments
   }
   ```
3. Map `FunctionResultContent` to `ToolResultBlock`:
   ```csharp
   {
       "type": "tool_result",
       "tool_use_id": resultContent.CallId,
       "content": resultContent.Result
   }
   ```

### Challenge 4: Multi-Modal Content
**Problem:** Different image/PDF handling mechanisms.

**Solution:**
1. For images:
   - Convert `DataContent` to base64
   - Map MIME type to Anthropic's `image_source.type` (base64)
   - Support image/jpeg, image/png, image/gif, image/webp
2. For PDFs:
   - Use Beta API for PDF support
   - Convert to base64 with `application/pdf` media type
   - Map to `PDFBlock` in beta messages

### Challenge 5: Model ID Mapping
**Problem:** Different model naming conventions.

**Solution:**
- Use model ID from `ChatOptions.ModelId` if provided
- Fall back to model ID from client initialization
- Support both full names (claude-3-5-sonnet-20241022) and aliases (claude-sonnet-4-5)
- Document model ID mapping in README

---

## 5. API Design Examples

### 5.1 🎯 Azure Foundry Basic Usage (PRIMARY)

```csharp
using Microsoft.Extensions.AI;
using Anthropic.Foundry;

// Option 1: API Key authentication
var credentials = new AnthropicFoundryApiKeyCredentials(
    apiKey: "your-api-key",
    resourceName: "your-azure-resource"
);
var foundryClient = new AnthropicFoundryClient(credentials);
IChatClient chatClient = new AnthropicChatClient(foundryClient);

// Option 2: Azure Identity (DefaultAzureCredential - recommended for production)
var credentials = await IAnthropicFoundryCredentials.FromEnv();
if (credentials != null)
{
    var foundryClient = new AnthropicFoundryClient(credentials);
    IChatClient chatClient = new AnthropicChatClient(foundryClient);
}

// Option 3: Bearer token
var credentials = new AnthropicFoundryBearerTokenCredentials(
    apiKey: "your-bearer-token",
    resourceName: "your-azure-resource"
);
var foundryClient = new AnthropicFoundryClient(credentials);
IChatClient chatClient = new AnthropicChatClient(foundryClient);

// Use the client
var response = await chatClient.GetResponseAsync(
    [new ChatMessage(ChatRole.User, "Hello, Claude!")],
    new ChatOptions
    {
        ModelId = "claude-sonnet-4-5",
        MaxOutputTokens = 1024
    });

Console.WriteLine(response.Message.Text);
```

### 5.2 Standard Anthropic Usage (SECONDARY)

```csharp
using Microsoft.Extensions.AI;
using Anthropic;

// Direct instantiation with standard API
var anthropicClient = new AnthropicClient(new ClientOptions
{
    APIKey = "sk-ant-..."
});
IChatClient chatClient = new AnthropicChatClient(anthropicClient);

// Use the client (same as Azure Foundry)
var response = await chatClient.GetResponseAsync(
    [new ChatMessage(ChatRole.User, "Hello, Claude!")]);
```

### 5.3 🎯 Azure Foundry Dependency Injection (PRIMARY)

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.AI;
using Anthropic.Foundry;

var services = new ServiceCollection();

// Option 1: From environment variables (ANTHROPIC_FOUNDRY_RESOURCE, ANTHROPIC_FOUNDRY_API_KEY)
services.AddAnthropicFoundryChatClientFromEnvironment(
    modelId: "claude-sonnet-4-5"
);

// Option 2: With explicit credentials
var credentials = new AnthropicFoundryApiKeyCredentials(
    apiKey: "your-api-key",
    resourceName: "your-azure-resource"
);
services.AddAnthropicFoundryChatClient(credentials, modelId: "claude-sonnet-4-5");

// Option 3: With resource name and API key
services.AddAnthropicFoundryChatClient(
    resourceName: "your-azure-resource",
    apiKey: "your-api-key",
    modelId: "claude-sonnet-4-5"
);

// Option 4: With configuration
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
var chatClient = services.BuildServiceProvider()
    .GetRequiredService<IChatClient>();
```

### 5.4 Standard Anthropic Dependency Injection (SECONDARY)

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.AI;

var services = new ServiceCollection();

// Simple registration with API key
services.AddAnthropicChatClient("sk-ant-...", modelId: "claude-sonnet-4-5");

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

### 5.3 Streaming

```csharp
await foreach (var update in chatClient.GetStreamingResponseAsync(
    [new ChatMessage(ChatRole.User, "Tell me a story")],
    new ChatOptions { ModelId = "claude-sonnet-4-5" }))
{
    foreach (var content in update.Contents.OfType<TextContent>())
    {
        Console.Write(content.Text);
    }
}
```

### 5.4 Tool Calling

```csharp
var getWeatherTool = AIFunctionFactory.Create((string location) =>
{
    return $"The weather in {location} is sunny, 72°F";
}, name: "get_weather", description: "Get the current weather");

var options = new ChatOptions
{
    ModelId = "claude-sonnet-4-5",
    Tools = [getWeatherTool],
    ToolMode = AutoChatToolMode.Instance
};

var response = await chatClient.GetResponseAsync(
    [new ChatMessage(ChatRole.User, "What's the weather in San Francisco?")],
    options);

// Check for tool calls
foreach (var toolCall in response.Message.Contents.OfType<FunctionCallContent>())
{
    Console.WriteLine($"Tool called: {toolCall.Name}");
}
```

---

## 6. File Structure

```
Microsoft.Extensions.AI.Anthropic/
├── src/
│   └── Microsoft.Extensions.AI.Anthropic/
│       ├── AnthropicChatClient.cs                    (Main IChatClient implementation)
│       ├── Converters/
│       │   ├── AnthropicMessageConverter.cs          (Message conversions)
│       │   ├── AnthropicContentConverter.cs          (Content block conversions)
│       │   ├── AnthropicOptionsConverter.cs          (Options/params conversions)
│       │   ├── AnthropicToolConverter.cs             (Tool/function conversions)
│       │   └── AnthropicStreamingConverter.cs        (Streaming event handling)
│       ├── Extensions/
│       │   ├── MicrosoftExtensionsAIAnthropicExtensions.cs        (Standard API DI extensions)
│       │   └── MicrosoftExtensionsAIAnthropicFoundryExtensions.cs (🎯 Azure Foundry DI extensions)
│       ├── Utilities/
│       │   ├── AnthropicModelMapper.cs               (Model ID mapping)
│       │   ├── AnthropicExceptionMapper.cs           (Exception handling)
│       │   └── AnthropicCredentialsHelper.cs         (🎯 Azure credentials helper utilities)
│       └── Microsoft.Extensions.AI.Anthropic.csproj
├── tests/
│   └── Microsoft.Extensions.AI.Anthropic.Tests/
│       ├── AnthropicChatClientTests.cs
│       ├── AnthropicFoundryChatClientTests.cs        (🎯 Azure Foundry specific tests)
│       ├── Converters/
│       │   ├── MessageConverterTests.cs
│       │   ├── ContentConverterTests.cs
│       │   ├── OptionsConverterTests.cs
│       │   ├── ToolConverterTests.cs
│       │   └── StreamingConverterTests.cs
│       ├── Integration/
│       │   ├── BasicChatTests.cs
│       │   ├── StreamingTests.cs
│       │   ├── ToolCallingTests.cs
│       │   ├── VisionTests.cs
│       │   └── AzureFoundryIntegrationTests.cs       (🎯 Azure-hosted API tests)
│       ├── Authentication/
│       │   ├── FoundryCredentialsTests.cs            (🎯 Azure auth tests)
│       │   └── EnvironmentConfigTests.cs             (🎯 Environment variable tests)
│       └── Microsoft.Extensions.AI.Anthropic.Tests.csproj
├── examples/
│   ├── AzureFoundryBasicExample/                     (🎯 PRIMARY - Azure Foundry basic chat)
│   ├── AzureFoundryStreamingExample/                 (🎯 PRIMARY - Azure Foundry streaming)
│   ├── AzureFoundryManagedIdentityExample/           (🎯 PRIMARY - Azure managed identity auth)
│   ├── BasicChatExample/                             (SECONDARY - Standard API)
│   ├── StreamingChatExample/                         (SECONDARY - Standard API)
│   ├── ToolCallingExample/
│   └── VisionExample/
├── docs/
│   ├── README.md
│   ├── AZURE-SETUP.md                                (🎯 Azure Foundry setup guide)
│   ├── AUTHENTICATION.md                             (🎯 Azure authentication options)
│   ├── MIGRATION.md                                  (Migration from direct Anthropic SDK)
│   └── research/
│       └── anthropic-integration-research-plan.md    (This document)
└── Microsoft.Extensions.AI.Anthropic.sln
```

---

## 7. Dependencies & Versions

### Required NuGet Packages

| Package | Version | Purpose | Priority |
|---|---|---|---|
| **`Anthropic.Foundry`** | **0.0.1+** | **🎯 Azure Anthropic Foundry client** | **PRIMARY** |
| `Anthropic` | 10.1.2+ | Official Anthropic C# SDK (transitive via Foundry) | PRIMARY |
| **`Azure.Identity`** | **1.17.0+** | **🎯 Azure authentication (DefaultAzureCredential, Managed Identity)** | **PRIMARY** |
| **`Microsoft.Rest.ClientRuntime.Azure.Authentication`** | **2.4.1+** | **🎯 Azure REST authentication support** | **PRIMARY** |
| `Microsoft.Extensions.AI.Abstractions` | Latest | Core abstractions (project reference) | REQUIRED |
| `System.Text.Json` | 9.0.9+ | JSON serialization | REQUIRED |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | Latest | DI support | OPTIONAL |
| `Microsoft.Extensions.Configuration.Abstractions` | Latest | Configuration binding | OPTIONAL |

### Target Frameworks
- `net8.0` - LTS support
- `net9.0` - Latest features
- `netstandard2.0` - Broad compatibility (match abstractions and Anthropic.Foundry)

**Note:** All target frameworks must match those supported by `Anthropic.Foundry` (net8.0, net9.0, netstandard2.0)

---

## 8. Testing Strategy

### 8.1 Unit Tests (90%+ coverage target)
- All converter methods
- Message/content mapping edge cases
- Options validation
- Error handling paths
- Null/empty input handling

### 8.2 Integration Tests

**🎯 PRIMARY: Azure Foundry Integration Tests**
- Real API calls to Azure-hosted Anthropic (gated by credentials)
- Authentication scenarios:
  - API Key authentication (`AnthropicFoundryApiKeyCredentials`)
  - Bearer token authentication (`AnthropicFoundryBearerTokenCredentials`)
  - Azure Identity authentication (`AnthropicFoundryIdentityTokenCredentials`)
  - Environment variable configuration
- All Claude model variants available on Azure (3.5 Sonnet, Opus 4, Haiku, etc.)
- Streaming scenarios on Azure endpoint
- Tool calling workflows
- Multi-turn conversations
- Rate limiting and retries
- Azure-specific error handling

**SECONDARY: Standard Anthropic Integration Tests**
- Real API calls to standard Anthropic API (gated by API key)
- Basic chat completion
- Streaming responses
- Tool calling
- Multi-modal inputs (images, PDFs)

### 8.3 Performance Tests
- Streaming throughput
- Memory allocations (benchmark streaming path)
- Large message handling
- Concurrent request handling

### 8.4 Compatibility Tests
- Cross-platform (Windows, Linux, macOS)
- Multiple .NET versions (8.0, 9.0)
- Different HttpClient configurations

---

## 9. Documentation Requirements

### 9.1 README.md
- Quick start guide
- Installation instructions
- Basic usage examples
- Streaming examples
- Tool calling examples
- Configuration options
- Model ID reference
- Troubleshooting

### 9.2 API Documentation
- XML documentation on all public APIs
- IntelliSense-friendly descriptions
- Code examples in XML comments
- Link to official Anthropic docs where relevant

### 9.3 Migration Guide
- Migrating from direct Anthropic SDK usage
- Differences from OpenAI implementation
- Feature parity matrix
- Known limitations

### 9.4 Architecture Documentation
- Type mapping reference
- Streaming event flow diagrams
- Error handling strategy
- Extension points for customization

---

## 10. Success Criteria

### 10.1 Functional Requirements
- ✅ Implements `IChatClient` interface completely
- ✅ Supports non-streaming chat completion
- ✅ Supports streaming chat completion
- ✅ Supports system messages
- ✅ Supports tool/function calling
- ✅ Supports multi-modal inputs (images, PDFs)
- ✅ Supports all Claude models
- ✅ Handles errors gracefully
- ✅ Preserves raw representations for debugging

### 10.2 Quality Requirements
- ✅ 90%+ code coverage
- ✅ Zero memory leaks
- ✅ Thread-safe implementation
- ✅ Efficient streaming (minimal allocations)
- ✅ Comprehensive XML documentation
- ✅ Follows .NET coding standards

### 10.3 Usability Requirements
- ✅ Simple API mirroring OpenAI implementation
- ✅ Intuitive DI registration
- ✅ Clear error messages
- ✅ Extensive examples
- ✅ Works with existing M.E.AI middleware/caching layers

### 10.4 Performance Requirements
- ✅ Streaming latency < 10ms per event
- ✅ Memory overhead < 5% vs direct SDK usage
- ✅ Supports 100+ concurrent requests

---

## 11. Risks & Mitigations

| Risk | Impact | Probability | Mitigation |
|---|---|---|---|
| Anthropic SDK breaking changes | HIGH | MEDIUM | Pin SDK version, monitor releases, abstract SDK types |
| M.E.AI abstractions evolution | HIGH | MEDIUM | Track abstractions repo, participate in discussions |
| Streaming complexity | MEDIUM | HIGH | Extensive testing, state machine validation |
| System message handling edge cases | MEDIUM | MEDIUM | Comprehensive test coverage for multi-system messages |
| Tool calling schema mismatches | MEDIUM | MEDIUM | JSON schema validation, robust error handling |
| Performance regression | MEDIUM | LOW | Benchmark suite, continuous profiling |
| Documentation drift | LOW | MEDIUM | Automated doc generation, CI checks |

---

## 12. Timeline & Milestones

### Milestone 1: Core Implementation (Week 1)
- Project setup
- Basic `AnthropicChatClient` implementation
- Message and options converters
- Non-streaming support working

### Milestone 2: Streaming & Tools (Week 2)
- Streaming implementation complete
- Tool calling support
- Content converters (images, PDFs)
- DI extensions

### Milestone 3: Testing & Polish (Week 3)
- Unit test suite complete
- Integration tests passing
- Example projects created
- Performance benchmarks established

### Milestone 4: Release Preparation (Week 4)
- Documentation complete
- Beta features support (thinking, caching)
- NuGet package prepared
- Migration guide finalized

---

## 13. Open Questions

### Azure Foundry Specific Questions

1. **🎯 Credential Management:**
   - Should we cache Azure Identity tokens or let Azure.Identity SDK handle it?
   - How to handle credential refresh for long-running applications?
   - Should we support custom token credential providers?

2. **🎯 Resource Name Configuration:**
   - Should resource name be settable per-request or only at client initialization?
   - How to handle multiple Azure regions/resources in same application?

3. **🎯 Azure-Specific Errors:**
   - How to distinguish Azure authentication errors from Anthropic API errors?
   - Should we wrap Azure.Identity exceptions or let them bubble up?

4. **🎯 Default Authentication Method:**
   - What should be the recommended default for production? (Managed Identity? DefaultAzureCredential?)
   - Should we provide guidance on different Azure deployment scenarios (App Service, Azure Functions, AKS, etc.)?

### General Questions

5. **System Message Concatenation:**
   - How should multiple system messages be combined? (Newlines, special delimiter?)
   - Should we preserve original system message boundaries in metadata?

6. **Model ID Defaults:**
   - What should be the default model if none specified? (claude-sonnet-4-5?)
   - Should we validate model IDs against known models?
   - Are model IDs different between Azure Foundry and standard API?

7. **Error Mapping:**
   - Should we create custom exception types or use standard exceptions?
   - How verbose should error messages be (include raw API response)?

8. **Beta Features:**
   - Should beta features require explicit opt-in?
   - How to handle beta API version headers?
   - Are beta features available on Azure Foundry?

9. **Caching Support:**
   - Should we expose Anthropic's prompt caching via additional properties?
   - How to integrate with M.E.AI caching middleware?

10. **Token Counting:**
    - Should we expose Anthropic's token counting API?
    - Where does it fit in the M.E.AI abstractions?
    - Is token counting available on Azure Foundry?

11. **Raw Representation:**
    - What level of detail for `RawRepresentation` properties?
    - Should we preserve full request/response for debugging?

---

## 14. References

### Documentation
- [Microsoft.Extensions.AI Documentation](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai)
- [Anthropic API Documentation](https://docs.anthropic.com/en/api)
- [Anthropic C# SDK Repository](https://github.com/anthropics/anthropic-sdk-csharp)

### Source Code References
- `Microsoft.Extensions.AI.Abstractions` - Core abstractions framework
- `Microsoft.Extensions.AI.OpenAI` - Reference implementation pattern
- Anthropic SDK `IMessageService` - Message API interface
- Anthropic SDK examples - Usage patterns

### Key Files Analyzed
- `IChatClient.cs:33-70` - Chat client interface definition
- `OpenAIChatClient.cs:27-794` - OpenAI reference implementation
- `AnthropicClient.cs:15-319` - Anthropic client architecture
- `IMessageService.cs:16-62` - Anthropic message API

---

## 15. Next Steps

After approval of this research plan:

1. **Create GitHub Repository/Branch**
   - Initialize project structure
   - Setup CI/CD pipeline
   - Configure code quality tools

2. **Begin Phase 1 Implementation**
   - Setup project file
   - Implement basic `AnthropicChatClient` shell
   - Create converter skeleton classes

3. **Setup Development Environment**
   - Obtain Anthropic API key for testing
   - Configure test project
   - Setup debugging configuration

4. **Establish Communication Channels**
   - Create tracking issues for each phase
   - Setup progress tracking
   - Identify stakeholders for reviews

---

**Document Version:** 2.0
**Last Updated:** 2025-11-19
**Author:** Research and Analysis Phase
**Status:** READY FOR REVIEW

---

## 16. Summary of Azure Foundry Focus

This implementation plan has been updated to **prioritize Azure Anthropic Foundry (AnthropicFoundryClient)** as the primary target, with the following key changes:

### Primary Changes
1. **Dependency on `Anthropic.Foundry` package** (v0.0.1+) as primary SDK
2. **Azure authentication support** via `Azure.Identity` and credential types
3. **Azure-specific base URL**: `https://{resourceName}.services.ai.azure.com/anthropic`
4. **Extended DI extensions** for Azure credential configuration
5. **Azure-focused examples** and documentation
6. **Azure-specific integration tests** for authentication scenarios

### Architecture Benefits
- **Enterprise-ready**: Built-in support for Azure Managed Identity and DefaultAzureCredential
- **Secure**: No hardcoded API keys, leverages Azure RBAC
- **Flexible**: Supports multiple authentication methods (API key, bearer token, Azure Identity)
- **Cloud-native**: Optimized for Azure deployments (App Service, Functions, AKS, etc.)

### Backward Compatibility
- Standard `AnthropicClient` (non-Azure) remains supported as secondary option
- All core functionality (chat, streaming, tools) works identically on both
- Migration path provided for users moving from standard API to Azure Foundry

**🎯 This plan is now aligned with Azure Anthropic Foundry as the primary integration target.**
