# API Reference - Microsoft.Extensions.AI.Anthropic

**Version**: 0.3.1-preview
**Last Updated**: 2025-01-19
**Namespace**: `Microsoft.Extensions.AI.Anthropic`

Complete API reference for all public types, methods, and extension methods in the Microsoft.Extensions.AI.Anthropic library.

## Table of Contents

- [AnthropicChatClient](#anthropicchatclient)
- [Extension Methods](#extension-methods)
  - [Azure Foundry Extensions](#azure-foundry-extensions)
  - [Standard API Extensions](#standard-api-extensions)
- [Configuration Types](#configuration-types)
- [Authentication](#authentication)
- [Converters (Internal)](#converters-internal)
- [Usage Examples](#usage-examples)

## AnthropicChatClient

The primary implementation of `IChatClient` for Anthropic's Claude models.

### Namespace

```csharp
using Microsoft.Extensions.AI.Anthropic;
```

### Class Declaration

```csharp
public sealed class AnthropicChatClient : IChatClient
```

### Constructors

#### Constructor 1: With IAnthropicClient

```csharp
public AnthropicChatClient(
    IAnthropicClient anthropicClient,
    string? modelId = null)
```

Creates an instance that wraps an Anthropic client (standard or Azure Foundry).

**Parameters**:
- `anthropicClient` - The Anthropic client instance (`AnthropicClient` or `AnthropicFoundryClient`)
- `modelId` - Optional default model ID (e.g., "claude-sonnet-4-5")

**Exceptions**:
- `ArgumentNullException` - If `anthropicClient` is null

**Example**:
```csharp
using Anthropic;
using Microsoft.Extensions.AI.Anthropic;

var anthropicClient = new AnthropicClient(new ClientOptions
{
    APIKey = "sk-ant-api03-..."
});

IChatClient chatClient = new AnthropicChatClient(
    anthropicClient,
    modelId: "claude-sonnet-4-5");
```

#### Constructor 2: With API Key and Resource Name

```csharp
public AnthropicChatClient(
    string apiKey,
    string resourceName,
    string? modelId = null)
```

Creates an instance with explicit API key authentication for Azure Foundry.

**Parameters**:
- `apiKey` - The API key for authentication
- `resourceName` - The Azure resource name
- `modelId` - Optional default model ID

**Exceptions**:
- `ArgumentNullException` - If `apiKey` is null

**Example**:
```csharp
var chatClient = new AnthropicChatClient(
    apiKey: "your-api-key",
    resourceName: "my-anthropic-resource",
    modelId: "claude-sonnet-4-5");
```

#### Constructor 3: With IMessageService

```csharp
public AnthropicChatClient(
    IMessageService messageService,
    string? modelId = null,
    Uri? endpoint = null,
    bool isAzureFoundry = false)
```

Creates an instance that wraps a message service directly (advanced scenarios).

**Parameters**:
- `messageService` - The Anthropic message service
- `modelId` - Optional default model ID
- `endpoint` - Optional endpoint URI for metadata
- `isAzureFoundry` - Whether this is Azure Foundry (true) or standard API (false)

**Exceptions**:
- `ArgumentNullException` - If `messageService` is null

### Properties

#### Metadata

```csharp
public ChatClientMetadata Metadata { get; }
```

Gets metadata about the chat client.

**Returns**: `ChatClientMetadata` containing:
- `ProviderName` - "anthropic" or "anthropic-foundry"
- `ProviderUri` - Endpoint URI (if available)
- `DefaultModelId` - Default model ID (if specified)

**Example**:
```csharp
var metadata = chatClient.Metadata;
Console.WriteLine($"Provider: {metadata.ProviderName}");
Console.WriteLine($"Default Model: {metadata.DefaultModelId}");
```

### Methods

#### GetResponseAsync

```csharp
public Task<ChatResponse> GetResponseAsync(
    IEnumerable<ChatMessage> chatMessages,
    ChatOptions? options = null,
    CancellationToken cancellationToken = default)
```

Sends a chat completion request to Claude and returns the complete response.

**Parameters**:
- `chatMessages` - The conversation messages
- `options` - Optional chat configuration options
- `cancellationToken` - Cancellation token

**Returns**: `Task<ChatResponse>` containing the complete response

**Exceptions**:
- `ArgumentNullException` - If `chatMessages` is null
- `InvalidOperationException` - If model ID not specified
- `HttpRequestException` - For API errors (401, 429, 500, etc.)

**Example**:
```csharp
var messages = new List<ChatMessage>
{
    new(ChatRole.System, "You are a helpful assistant."),
    new(ChatRole.User, "What is .NET?")
};

var options = new ChatOptions
{
    ModelId = "claude-sonnet-4-5",
    Temperature = 0.7f,
    MaxOutputTokens = 1024
};

var response = await chatClient.GetResponseAsync(messages, options);

Console.WriteLine($"Response: {response.Text}");
Console.WriteLine($"Finish Reason: {response.FinishReason}");
Console.WriteLine($"Model: {response.ModelId}");

// Access usage information
var usage = response.Messages[0].Contents.OfType<UsageContent>().FirstOrDefault();
if (usage != null)
{
    Console.WriteLine($"Total Tokens: {usage.Details.TotalTokenCount}");
}
```

#### GetStreamingResponseAsync

```csharp
public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
    IEnumerable<ChatMessage> chatMessages,
    ChatOptions? options = null,
    CancellationToken cancellationToken = default)
```

Sends a chat completion request and streams the response in real-time.

**Parameters**:
- `chatMessages` - The conversation messages
- `options` - Optional chat configuration options
- `cancellationToken` - Cancellation token

**Returns**: `IAsyncEnumerable<ChatResponseUpdate>` - Stream of response updates

**Exceptions**:
- `ArgumentNullException` - If `chatMessages` is null
- `InvalidOperationException` - If model ID not specified
- `HttpRequestException` - For API errors

**Example**:
```csharp
var messages = new[] { new ChatMessage(ChatRole.User, "Write a short poem.") };

Console.Write("Claude: ");
await foreach (var update in chatClient.GetStreamingResponseAsync(messages))
{
    if (update.Text != null)
    {
        Console.Write(update.Text);
    }

    // Check for finish reason
    if (update.FinishReason != null)
    {
        Console.WriteLine($"\n\nFinish Reason: {update.FinishReason}");
    }

    // Access usage (in final update)
    if (update.Contents.OfType<UsageContent>().Any())
    {
        var usage = update.Contents.OfType<UsageContent>().First();
        Console.WriteLine($"Tokens: {usage.Details.TotalTokenCount}");
    }
}
```

#### GetService

```csharp
public object? GetService(
    Type serviceType,
    object? serviceKey = null)
```

Gets a service object of the specified type (service locator pattern).

**Parameters**:
- `serviceType` - The type of service to retrieve
- `serviceKey` - Optional service key

**Returns**: Service instance or `null` if not available

**Supported Services**:
- `IChatClient` - Returns self
- `AnthropicChatClient` - Returns self
- `ChatClientMetadata` - Returns metadata
- `IAnthropicClient` - Returns underlying Anthropic client
- `IMessageService` - Returns message service

**Example**:
```csharp
// Get metadata
var metadata = chatClient.GetService(typeof(ChatClientMetadata)) as ChatClientMetadata;

// Get underlying Anthropic client
var anthropicClient = chatClient.GetService(typeof(IAnthropicClient)) as IAnthropicClient;
```

#### Dispose

```csharp
public void Dispose()
```

Releases resources used by the chat client.

**Example**:
```csharp
using var chatClient = new AnthropicChatClient(client, "claude-sonnet-4-5");
// Use chatClient
// Automatically disposed
```

## Extension Methods

### Azure Foundry Extensions

Located in `Microsoft.Extensions.AI.Anthropic.Extensions` namespace.

#### AddAnthropicFoundryChatClientFromEnvironment (IServiceCollection)

```csharp
public static IServiceCollection AddAnthropicFoundryChatClientFromEnvironment(
    this IServiceCollection services,
    string? resourceName = null,
    string? modelId = null)
```

Registers `IChatClient` using environment variables for configuration.

**Environment Variables**:
- `ANTHROPIC_FOUNDRY_RESOURCE` - Azure resource name (required if `resourceName` is null)
- `ANTHROPIC_FOUNDRY_API_KEY` - API key (optional, uses Azure Identity if not set)

**Parameters**:
- `services` - The service collection
- `resourceName` - Optional resource name (overrides environment variable)
- `modelId` - Optional default model ID

**Returns**: `IServiceCollection` for chaining

**Example**:
```csharp
// Read from environment variables
builder.Services.AddAnthropicFoundryChatClientFromEnvironment(
    modelId: "claude-sonnet-4-5");

// Override resource name
builder.Services.AddAnthropicFoundryChatClientFromEnvironment(
    resourceName: "my-resource",
    modelId: "claude-sonnet-4-5");
```

#### AddAnthropicFoundryChatClient (IServiceCollection)

```csharp
public static IServiceCollection AddAnthropicFoundryChatClient(
    this IServiceCollection services,
    string resourceName,
    string apiKey,
    string? modelId = null)
```

Registers `IChatClient` with explicit API key authentication.

**Parameters**:
- `services` - The service collection
- `resourceName` - Azure resource name
- `apiKey` - API key for authentication
- `modelId` - Optional default model ID

**Returns**: `IServiceCollection` for chaining

**Example**:
```csharp
builder.Services.AddAnthropicFoundryChatClient(
    resourceName: "my-anthropic-resource",
    apiKey: "your-api-key",
    modelId: "claude-sonnet-4-5");
```

#### AddAnthropicFoundryChatClient (with credentials)

```csharp
public static IServiceCollection AddAnthropicFoundryChatClient(
    this IServiceCollection services,
    IAnthropicFoundryCredentials credentials,
    string? modelId = null)
```

Registers `IChatClient` with custom credentials.

**Parameters**:
- `services` - The service collection
- `credentials` - Azure Foundry credentials (API key, bearer token, or identity)
- `modelId` - Optional default model ID

**Returns**: `IServiceCollection` for chaining

**Example**:
```csharp
using Anthropic.Foundry;
using Azure.Identity;

var credentials = new AnthropicFoundryIdentityTokenCredentials(
    tokenCredential: new DefaultAzureCredential(),
    resourceName: "my-anthropic-resource");

builder.Services.AddAnthropicFoundryChatClient(
    credentials: credentials,
    modelId: "claude-sonnet-4-5");
```

#### AddAnthropicFoundryChatClient (with factory)

```csharp
public static IServiceCollection AddAnthropicFoundryChatClient(
    this IServiceCollection services,
    Func<IServiceProvider, AnthropicFoundryClient> clientFactory,
    string? modelId = null)
```

Registers `IChatClient` with a factory function for custom client creation.

**Parameters**:
- `services` - The service collection
- `clientFactory` - Factory function to create the client
- `modelId` - Optional default model ID

**Returns**: `IServiceCollection` for chaining

**Example**:
```csharp
builder.Services.AddAnthropicFoundryChatClient(
    clientFactory: sp =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        var credentials = new AnthropicFoundryApiKeyCredentials(
            apiKey: config["Azure:Anthropic:ApiKey"],
            resourceName: config["Azure:Anthropic:ResourceName"]);
        return new AnthropicFoundryClient(credentials);
    },
    modelId: "claude-sonnet-4-5");
```

#### UseAnthropicFoundryFromEnvironment (IChatClientBuilder)

```csharp
public static IChatClientBuilder UseAnthropicFoundryFromEnvironment(
    this IChatClientBuilder builder,
    string? resourceName = null,
    string? modelId = null)
```

Adds Azure Foundry chat client to the builder pipeline.

**Parameters**:
- `builder` - The chat client builder
- `resourceName` - Optional resource name
- `modelId` - Optional default model ID

**Returns**: `IChatClientBuilder` for chaining

**Example**:
```csharp
builder.Services.AddChatClient(chatBuilder => chatBuilder
    .UseAnthropicFoundryFromEnvironment(modelId: "claude-sonnet-4-5")
    .UseLogging()
    .UseOpenTelemetry());
```

### Standard API Extensions

#### AddAnthropicChatClient (IServiceCollection)

```csharp
public static IServiceCollection AddAnthropicChatClient(
    this IServiceCollection services,
    string apiKey,
    string? modelId = null)
```

Registers `IChatClient` for standard Anthropic API with API key.

**Parameters**:
- `services` - The service collection
- `apiKey` - Anthropic API key
- `modelId` - Optional default model ID

**Returns**: `IServiceCollection` for chaining

**Example**:
```csharp
builder.Services.AddAnthropicChatClient(
    apiKey: "sk-ant-api03-...",
    modelId: "claude-sonnet-4-5");
```

#### AddAnthropicChatClient (with factory)

```csharp
public static IServiceCollection AddAnthropicChatClient(
    this IServiceCollection services,
    Func<IServiceProvider, AnthropicClient> clientFactory,
    string? modelId = null)
```

Registers `IChatClient` with a factory function.

**Parameters**:
- `services` - The service collection
- `clientFactory` - Factory function to create the client
- `modelId` - Optional default model ID

**Returns**: `IServiceCollection` for chaining

**Example**:
```csharp
builder.Services.AddAnthropicChatClient(
    clientFactory: sp =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        return new AnthropicClient(new ClientOptions
        {
            APIKey = config["Anthropic:ApiKey"]
        });
    },
    modelId: "claude-sonnet-4-5");
```

#### UseAnthropicChatClient (IChatClientBuilder)

```csharp
public static IChatClientBuilder UseAnthropicChatClient(
    this IChatClientBuilder builder,
    string apiKey,
    string? modelId = null)
```

Adds standard Anthropic chat client to the builder pipeline.

**Parameters**:
- `builder` - The chat client builder
- `apiKey` - Anthropic API key
- `modelId` - Optional default model ID

**Returns**: `IChatClientBuilder` for chaining

**Example**:
```csharp
builder.Services.AddChatClient(chatBuilder => chatBuilder
    .UseAnthropicChatClient(apiKey: "sk-ant-api03-...", modelId: "claude-sonnet-4-5")
    .UseLogging()
    .UseRetryPolicy());
```

## Configuration Types

### ChatOptions

Configuration options for chat requests (from `Microsoft.Extensions.AI`).

```csharp
public class ChatOptions
{
    public string? ModelId { get; set; }
    public float? Temperature { get; set; }
    public float? TopP { get; set; }
    public int? TopK { get; set; }
    public int? MaxOutputTokens { get; set; }
    public IList<string>? StopSequences { get; set; }
    public IList<AITool>? Tools { get; set; }
    public ChatToolMode? ToolMode { get; set; }
    public IDictionary<string, object?>? AdditionalProperties { get; set; }
}
```

**Example**:
```csharp
var options = new ChatOptions
{
    ModelId = "claude-sonnet-4-5",
    Temperature = 0.7f,                              // 0.0 - 1.0
    TopP = 0.9f,                                     // Nucleus sampling
    TopK = 40,                                       // Top-K sampling
    MaxOutputTokens = 2048,                          // Max response length
    StopSequences = ["STOP", "END"],                 // Stop sequences
    Tools = [weatherTool, searchTool],               // Function calling
    ToolMode = AutoChatToolMode.Instance,            // Auto tool usage
    AdditionalProperties = new Dictionary<string, object?>
    {
        ["thinking"] = new { type = "enabled", budget_tokens = 10000 }  // Extended thinking
    }
};
```

### ChatMessage

Represents a message in the conversation (from `Microsoft.Extensions.AI`).

```csharp
public class ChatMessage
{
    public ChatRole Role { get; set; }
    public IList<AIContent> Contents { get; set; }

    // Convenience constructors
    public ChatMessage(ChatRole role, string text)
    public ChatMessage(ChatRole role, IEnumerable<AIContent> contents)
}
```

**Roles**:
- `ChatRole.System` - System instructions (automatically combined)
- `ChatRole.User` - User messages
- `ChatRole.Assistant` - Claude's responses
- `ChatRole.Tool` - Tool/function results

**Example**:
```csharp
var messages = new List<ChatMessage>
{
    new(ChatRole.System, "You are a helpful assistant."),
    new(ChatRole.User, "What is C#?"),
    new(ChatRole.Assistant, "C# is a modern, object-oriented programming language..."),
    new(ChatRole.User, "What are its main features?")
};
```

### AIContent Types

Content types for multi-modal interactions.

#### TextContent

```csharp
var textContent = new TextContent("Hello, Claude!");
```

#### DataContent (Images)

```csharp
var imageBytes = await File.ReadAllBytesAsync("diagram.png");
var imageContent = new DataContent(imageBytes, "image/png");

var message = new ChatMessage(ChatRole.User, [
    new TextContent("What's in this image?"),
    imageContent
]);
```

**Supported MIME Types**:
- `image/jpeg`
- `image/png`
- `image/gif`
- `image/webp`
- `application/pdf` (Beta API, Claude Opus 4 only)

#### FunctionCallContent (Tool Calls)

```csharp
var toolCall = new FunctionCallContent(
    callId: "call_123",
    name: "get_weather",
    arguments: new Dictionary<string, object> { ["location"] = "San Francisco" });
```

#### FunctionResultContent (Tool Results)

```csharp
var toolResult = new FunctionResultContent(
    callId: "call_123",
    name: "get_weather",
    result: "Sunny, 72°F");
```

## Authentication

### Azure Foundry Credentials

#### AnthropicFoundryApiKeyCredentials

```csharp
using Anthropic.Foundry;

var credentials = new AnthropicFoundryApiKeyCredentials(
    apiKey: "your-api-key",
    resourceName: "my-anthropic-resource");

var client = new AnthropicFoundryClient(credentials);
```

#### AnthropicFoundryIdentityTokenCredentials

```csharp
using Anthropic.Foundry;
using Azure.Identity;

var credentials = new AnthropicFoundryIdentityTokenCredentials(
    tokenCredential: new DefaultAzureCredential(),
    resourceName: "my-anthropic-resource");

var client = new AnthropicFoundryClient(credentials);
```

#### AnthropicFoundryBearerTokenCredentials

```csharp
using Anthropic.Foundry;

var credentials = new AnthropicFoundryBearerTokenCredentials(
    bearerToken: "your-bearer-token",
    resourceName: "my-anthropic-resource");

var client = new AnthropicFoundryClient(credentials);
```

### Standard API Authentication

```csharp
using Anthropic;

var client = new AnthropicClient(new ClientOptions
{
    APIKey = "sk-ant-api03-..."
});
```

## Converters (Internal)

These are internal implementation details but documented for completeness.

### AnthropicMessageConverter

Converts between `ChatMessage` and Anthropic's `MessageParam`.

**Key Features**:
- Extracts system messages and combines them
- Converts role mappings (User/Assistant/Tool)
- Handles multi-modal content

### AnthropicContentConverter

Converts between `AIContent` and Anthropic's `ContentBlock`.

**Supported Conversions**:
- `TextContent` ↔ `TextBlock`
- `DataContent` (images) ↔ `ImageBlock`
- `DataContent` (PDFs) ↔ `PDFBlock`
- `FunctionCallContent` ↔ `ToolUseBlock`
- `FunctionResultContent` ↔ `ToolResultBlock`

### AnthropicOptionsConverter

Converts `ChatOptions` to Anthropic's `MessageCreateParams`.

**Mapped Properties**:
- Temperature, TopP, TopK
- MaxOutputTokens → max_tokens
- StopSequences → stop_sequences
- Tools → tool definitions
- ToolMode → tool_choice

### AnthropicToolConverter

Converts `AIFunctionDeclaration` to Anthropic's `ToolDefinition` with JSON schema.

**Features**:
- Automatic schema generation
- Type inference (string, int, bool, array, object)
- Enum support with constraints
- Required parameter tracking

### AnthropicStreamingConverter

Converts Anthropic's streaming events to `ChatResponseUpdate`.

**Event Types**:
- `message_start` - Begin message
- `content_block_start` - Begin content block
- `content_block_delta` - Text/tool deltas
- `message_delta` - Usage updates
- `message_stop` - End message

## Usage Examples

### Example 1: Basic Chat with Error Handling

```csharp
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Anthropic;

var chatClient = new AnthropicChatClient(
    apiKey: "your-api-key",
    resourceName: "my-resource",
    modelId: "claude-sonnet-4-5");

try
{
    var messages = new[] { new ChatMessage(ChatRole.User, "What is .NET?") };
    var response = await chatClient.GetResponseAsync(messages);
    Console.WriteLine(response.Text);
}
catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
{
    Console.WriteLine("Authentication failed. Check your API key.");
}
catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
{
    Console.WriteLine("Rate limit exceeded. Retry later.");
}
finally
{
    chatClient.Dispose();
}
```

### Example 2: Streaming with Progress

```csharp
var messages = new[] { new ChatMessage(ChatRole.User, "Write a story about coding.") };

var textBuilder = new StringBuilder();
await foreach (var update in chatClient.GetStreamingResponseAsync(messages))
{
    if (update.Text != null)
    {
        textBuilder.Append(update.Text);
        Console.Write(update.Text);
    }

    if (update.FinishReason == ChatFinishReason.Stop)
    {
        Console.WriteLine($"\n\nComplete response:\n{textBuilder}");
    }
}
```

### Example 3: Multi-turn Conversation

```csharp
var conversation = new List<ChatMessage>
{
    new(ChatRole.System, "You are a coding tutor."),
    new(ChatRole.User, "What is a lambda expression in C#?")
};

var response1 = await chatClient.GetResponseAsync(conversation);
conversation.Add(response1.Message);

conversation.Add(new ChatMessage(ChatRole.User, "Can you show me an example?"));
var response2 = await chatClient.GetResponseAsync(conversation);

Console.WriteLine($"Answer: {response2.Text}");
```

### Example 4: Function Calling

```csharp
var weatherTool = AIFunctionFactory.Create(
    (string location) => $"Weather in {location}: Sunny, 72°F",
    name: "get_weather",
    description: "Get current weather for a location");

var options = new ChatOptions
{
    Tools = [weatherTool],
    ToolMode = AutoChatToolMode.Instance
};

var messages = new[] { new ChatMessage(ChatRole.User, "What's the weather in Seattle?") };
var response = await chatClient.GetResponseAsync(messages, options);

// Check for tool calls
foreach (var toolCall in response.Message.Contents.OfType<FunctionCallContent>())
{
    Console.WriteLine($"Tool called: {toolCall.Name}");
    Console.WriteLine($"Arguments: {JsonSerializer.Serialize(toolCall.Arguments)}");
}
```

### Example 5: Image Analysis

```csharp
var imageBytes = await File.ReadAllBytesAsync("diagram.png");

var message = new ChatMessage(ChatRole.User, [
    new TextContent("Describe this image in detail."),
    new DataContent(imageBytes, "image/png")
]);

var response = await chatClient.GetResponseAsync([message]);
Console.WriteLine(response.Text);
```

---

**See Also**:
- [Getting Started Guide](GETTING-STARTED.md)
- [Authentication Guide](AUTHENTICATION-GUIDE.md)
- [Examples Guide](EXAMPLES-GUIDE.md)
- [Architecture Documentation](ARCHITECTURE.md)
