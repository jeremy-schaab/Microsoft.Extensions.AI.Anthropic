# Getting Started with Microsoft.Extensions.AI.Anthropic

**Version**: 0.3.1-preview
**Last Updated**: 2025-01-19
**Target Audience**: .NET developers new to Anthropic Claude integration

Welcome to **Microsoft.Extensions.AI.Anthropic** - the official integration library that brings Anthropic's Claude AI models to the Microsoft.Extensions.AI abstractions framework. This guide will help you get started in under 10 minutes.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [Quick Start](#quick-start)
  - [Azure Foundry (Recommended)](#azure-foundry-recommended)
  - [Standard Anthropic API](#standard-anthropic-api)
- [Your First Chat](#your-first-chat)
- [Configuration](#configuration)
- [Common Patterns](#common-patterns)
- [Troubleshooting](#troubleshooting)
- [Next Steps](#next-steps)

## Prerequisites

Before you begin, ensure you have:

### 1. .NET 9.0 SDK

Install the latest .NET SDK:

```bash
# Check if you have .NET 9.0
dotnet --version

# Should output: 9.0.x or higher
```

**Download**: [https://dotnet.microsoft.com/download](https://dotnet.microsoft.com/download)

### 2. API Access

Choose one of these options:

**Option A: Azure Anthropic Foundry** (Recommended for Production)
- Azure subscription with Anthropic Foundry access
- Resource name (e.g., `my-anthropic-resource`)
- API key OR Azure Identity (Managed Identity)

**Option B: Standard Anthropic API**
- Anthropic API account
- API key from https://console.anthropic.com/

### 3. Development Environment

Any of these will work:
- **Visual Studio 2022** (v17.9 or later)
- **Visual Studio Code** with C# Dev Kit
- **JetBrains Rider** (2024.1 or later)
- **Command line** with any text editor

## Installation

### NuGet Package

Install the library via NuGet Package Manager:

**Package Manager Console**:
```powershell
Install-Package Microsoft.Extensions.AI.Anthropic -Version 0.3.1-preview
```

**.NET CLI**:
```bash
dotnet add package Microsoft.Extensions.AI.Anthropic --version 0.3.1-preview
```

**Visual Studio**:
1. Right-click project → Manage NuGet Packages
2. Search for `Microsoft.Extensions.AI.Anthropic`
3. Click Install

### What Gets Installed

The package includes:
- `Microsoft.Extensions.AI.Anthropic` - Main library
- `Anthropic.Foundry` - Azure Foundry client (embedded)
- `Anthropic` - Standard Anthropic SDK (embedded)
- `Azure.Identity` - Azure authentication support
- `Microsoft.Extensions.AI.Abstractions` - Core AI abstractions

**Note**: The Anthropic SDKs are embedded in the package, so you get a single DLL with no external dependencies on Anthropic packages.

## Quick Start

### Azure Foundry (Recommended)

Azure Foundry provides enterprise-grade features including Managed Identity, private endpoints, and compliance certifications.

#### Step 1: Set Environment Variables

**Windows (PowerShell)**:
```powershell
$env:ANTHROPIC_FOUNDRY_RESOURCE="your-resource-name"
$env:ANTHROPIC_FOUNDRY_API_KEY="your-api-key"
```

**Linux/macOS**:
```bash
export ANTHROPIC_FOUNDRY_RESOURCE="your-resource-name"
export ANTHROPIC_FOUNDRY_API_KEY="your-api-key"
```

#### Step 2: Create a Console Application

```bash
dotnet new console -n MyFirstClaude
cd MyFirstClaude
dotnet add package Microsoft.Extensions.AI.Anthropic --version 0.3.1-preview
```

#### Step 3: Write Your First Code

Replace the contents of `Program.cs`:

```csharp
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Anthropic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// Register chat client from environment variables
builder.Services.AddAnthropicFoundryChatClientFromEnvironment(
    modelId: "claude-sonnet-4-5");

var host = builder.Build();
var chatClient = host.Services.GetRequiredService<IChatClient>();

// Send your first message
var response = await chatClient.GetResponseAsync(
    "What are the three most popular programming languages in 2025?");

Console.WriteLine(response.Text);
```

#### Step 4: Run It

```bash
dotnet run
```

**Expected Output**:
```
Based on current trends, the three most popular programming languages in 2025 are:

1. Python - Dominant in AI/ML, data science, and web development
2. JavaScript/TypeScript - Essential for web development and full-stack applications
3. C# - Strong in enterprise development, game development (Unity), and cloud applications

Each has strong ecosystems and community support...
```

### Standard Anthropic API

If you're using the standard Anthropic API instead of Azure Foundry:

#### Step 1: Set API Key

```bash
# Windows
set ANTHROPIC_API_KEY=sk-ant-api03-...

# Linux/macOS
export ANTHROPIC_API_KEY=sk-ant-api03-...
```

#### Step 2: Code Example

```csharp
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Anthropic;
using Anthropic;

// Create Anthropic client
var anthropicClient = new AnthropicClient(new ClientOptions
{
    APIKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")
});

// Wrap with Microsoft.Extensions.AI
IChatClient chatClient = new AnthropicChatClient(
    anthropicClient,
    modelId: "claude-sonnet-4-5");

// Send a message
var response = await chatClient.GetResponseAsync(
    "Explain quantum computing in one sentence.");

Console.WriteLine(response.Text);
```

## Your First Chat

Let's build a more complete chat application with conversation history and system messages.

### Complete Example

```csharp
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Anthropic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// Register chat client
builder.Services.AddAnthropicFoundryChatClientFromEnvironment(
    modelId: "claude-sonnet-4-5");

var host = builder.Build();
var chatClient = host.Services.GetRequiredService<IChatClient>();

Console.WriteLine("Chat with Claude - Type 'exit' to quit");
Console.WriteLine("========================================\n");

// Conversation history
var messages = new List<ChatMessage>
{
    new(ChatRole.System, "You are a helpful coding assistant. Provide clear, concise answers with code examples when appropriate.")
};

while (true)
{
    Console.Write("You: ");
    var userInput = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(userInput) || userInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
        break;

    // Add user message to history
    messages.Add(new ChatMessage(ChatRole.User, userInput));

    // Get response with full conversation context
    var response = await chatClient.GetResponseAsync(messages, new ChatOptions
    {
        Temperature = 0.7f,
        MaxOutputTokens = 1024
    });

    // Add assistant response to history
    messages.Add(response.Message);

    Console.WriteLine($"\nClaude: {response.Text}\n");

    // Display usage statistics
    var usage = response.Messages[0].Contents.OfType<UsageContent>().FirstOrDefault();
    if (usage != null)
    {
        Console.WriteLine($"[Tokens: {usage.Details.TotalTokenCount}]");
    }
}

Console.WriteLine("Goodbye!");
```

### What This Example Shows

1. **System Messages**: Guide Claude's behavior
2. **Conversation History**: Maintain context across multiple turns
3. **Chat Options**: Control temperature and max tokens
4. **Usage Tracking**: Monitor token consumption
5. **Type Safety**: Full IntelliSense support

## Configuration

### Model Selection

Choose the right model for your use case:

```csharp
var options = new ChatOptions
{
    ModelId = "claude-sonnet-4-5",  // Balanced (recommended for most tasks)
    // ModelId = "claude-opus-4",   // Complex reasoning, best quality
    // ModelId = "claude-haiku-4",  // Fast, cost-effective
};

var response = await chatClient.GetResponseAsync(messages, options);
```

**Model Comparison**:

| Model ID | Speed | Quality | Cost | Best For |
|----------|-------|---------|------|----------|
| `claude-haiku-4` | Fastest | Good | Lowest | Simple tasks, high volume |
| `claude-sonnet-4-5` | Fast | Excellent | Medium | General purpose (default) |
| `claude-opus-4` | Slower | Best | Highest | Complex reasoning, code generation |

### Temperature and Sampling

Control randomness and creativity:

```csharp
var options = new ChatOptions
{
    Temperature = 0.0f,    // Deterministic (0.0 - 1.0)
    TopP = 0.9f,           // Nucleus sampling
    TopK = 40,             // Top-K sampling
    MaxOutputTokens = 2048 // Max response length
};
```

**Temperature Guidelines**:
- **0.0 - 0.3**: Factual, deterministic (code generation, analysis)
- **0.4 - 0.7**: Balanced (general conversation)
- **0.8 - 1.0**: Creative (brainstorming, creative writing)

### System Messages

Guide Claude's behavior and personality:

```csharp
var messages = new List<ChatMessage>
{
    // Multiple system messages are automatically combined
    new(ChatRole.System, "You are a Python expert specializing in data science."),
    new(ChatRole.System, "Always provide working code examples."),
    new(ChatRole.System, "Explain concepts clearly for beginners."),
    new(ChatRole.User, "How do I read a CSV file in Python?")
};

var response = await chatClient.GetResponseAsync(messages);
```

## Common Patterns

### Pattern 1: Streaming Responses

Stream tokens in real-time for better user experience:

```csharp
var messages = new[] { new ChatMessage(ChatRole.User, "Write a short poem about coding.") };

Console.Write("Claude: ");
await foreach (var update in chatClient.GetStreamingResponseAsync(messages))
{
    Console.Write(update.Text);
}
Console.WriteLine();
```

**Output**:
```
Claude: Lines of code dance on the screen,
Logic flows where bugs convene...
```

### Pattern 2: Dependency Injection

Integrate with ASP.NET Core or any DI container:

```csharp
// Startup.cs or Program.cs
builder.Services.AddAnthropicFoundryChatClientFromEnvironment(
    modelId: "claude-sonnet-4-5");

// Use in controllers, services, etc.
public class ChatService
{
    private readonly IChatClient _chatClient;

    public ChatService(IChatClient chatClient)
    {
        _chatClient = chatClient;
    }

    public async Task<string> GetResponseAsync(string userMessage)
    {
        var response = await _chatClient.GetResponseAsync(userMessage);
        return response.Text;
    }
}
```

### Pattern 3: Error Handling

Robust error handling for production applications:

```csharp
try
{
    var response = await chatClient.GetResponseAsync(messages);
    Console.WriteLine(response.Text);
}
catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
{
    Console.WriteLine("Rate limit exceeded. Please retry in a few seconds.");
}
catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
{
    Console.WriteLine("Authentication failed. Check your API key.");
}
catch (InvalidOperationException ex) when (ex.Message.Contains("Model ID"))
{
    Console.WriteLine("Model ID not specified. Provide it in constructor or options.");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
```

### Pattern 4: Middleware Pipeline

Add logging, caching, retries, and telemetry:

```csharp
builder.Services.AddChatClient(chatBuilder => chatBuilder
    .UseAnthropicFoundryFromEnvironment(modelId: "claude-sonnet-4-5")
    .UseLogging()           // Log requests/responses
    .UseOpenTelemetry()     // Add telemetry
    .UseRetryPolicy());     // Automatic retries
```

## Troubleshooting

### Issue: "Model ID must be specified"

**Error**:
```
InvalidOperationException: Model ID must be specified either in the constructor or in ChatOptions.ModelId
```

**Solution**: Provide model ID in constructor OR options:

```csharp
// Option 1: Constructor
var chatClient = new AnthropicChatClient(client, modelId: "claude-sonnet-4-5");

// Option 2: ChatOptions
var options = new ChatOptions { ModelId = "claude-sonnet-4-5" };
var response = await chatClient.GetResponseAsync(messages, options);
```

### Issue: "ANTHROPIC_FOUNDRY_RESOURCE environment variable is not set"

**Solution**: Set required environment variables:

```bash
# Windows
set ANTHROPIC_FOUNDRY_RESOURCE=your-resource-name

# Linux/macOS
export ANTHROPIC_FOUNDRY_RESOURCE=your-resource-name
```

### Issue: Authentication Failed (401)

**Causes and Solutions**:

1. **Invalid API Key**
   - Check key in Azure Portal (Azure Foundry)
   - Check key in Anthropic Console (standard API)
   - Ensure no extra spaces or line breaks

2. **Azure Identity Failed**
   - Run `az login` for local development
   - Enable Managed Identity for Azure App Service/Functions
   - Verify RBAC permissions

3. **Wrong Resource Name**
   - Verify resource name in Azure Portal
   - Ensure no typos in environment variable

### Issue: Rate Limit Exceeded (429)

**Solution**: Implement retry logic with exponential backoff:

```csharp
int maxRetries = 3;
int retryCount = 0;
TimeSpan delay = TimeSpan.FromSeconds(1);

while (retryCount < maxRetries)
{
    try
    {
        var response = await chatClient.GetResponseAsync(messages);
        return response.Text;
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
    {
        retryCount++;
        if (retryCount >= maxRetries) throw;

        await Task.Delay(delay);
        delay = TimeSpan.FromSeconds(delay.TotalSeconds * 2); // Exponential backoff
    }
}
```

### Issue: "The model 'xxx' is not supported"

**Solution**: Check available models for your deployment:

**Azure Foundry**:
- Verify model availability in Azure Portal
- Model IDs may differ from standard API

**Standard API**:
- Check [Anthropic documentation](https://docs.anthropic.com/en/docs/models-overview)
- Use versioned model IDs (e.g., `claude-3-5-sonnet-20241022`)

### Issue: Slow Response Times

**Solutions**:

1. **Use Faster Models**:
   ```csharp
   ModelId = "claude-haiku-4"  // Fastest model
   ```

2. **Reduce Max Tokens**:
   ```csharp
   MaxOutputTokens = 512  // Shorter responses
   ```

3. **Use Streaming**:
   ```csharp
   await foreach (var update in chatClient.GetStreamingResponseAsync(messages))
   {
       Console.Write(update.Text);  // Show tokens as they arrive
   }
   ```

## Next Steps

Congratulations! You now have a working Anthropic Claude integration. Here's what to explore next:

### 1. Learn Advanced Features

- **[API Reference](API-REFERENCE.md)** - Complete API documentation
- **[Authentication Guide](AUTHENTICATION-GUIDE.md)** - Production authentication patterns
- **[Examples Guide](EXAMPLES-GUIDE.md)** - Sample applications

### 2. Explore Examples

Run the example projects in `examples/`:

```bash
# Azure Foundry basic example
cd examples/AzureFoundryBasicExample
dotnet run

# Streaming chat
cd examples/StreamingChatExample
dotnet run

# Tool calling (function calling)
cd examples/ToolCallingExample
dotnet run

# Vision (image analysis)
cd examples/VisionExample
dotnet run
```

### 3. Add Advanced Capabilities

**Function Calling**: Let Claude use tools
```csharp
var weatherTool = AIFunctionFactory.Create(
    (string location) => GetWeather(location),
    name: "get_weather");

var options = new ChatOptions { Tools = [weatherTool] };
```

**Vision**: Analyze images
```csharp
var imageBytes = File.ReadAllBytes("diagram.png");
var message = new ChatMessage(ChatRole.User, [
    new TextContent("What's in this image?"),
    new DataContent(imageBytes, "image/png")
]);
```

**Multi-turn Conversations**: Maintain context
```csharp
var messages = new List<ChatMessage>
{
    new(ChatRole.System, "You are a helpful assistant."),
    new(ChatRole.User, "What is .NET?"),
    new(ChatRole.Assistant, response1.Message.Text),
    new(ChatRole.User, "What are its main features?")
};
```

### 4. Deploy to Production

- **[Architecture Documentation](ARCHITECTURE.md)** - System design
- **Azure Deployment**: App Service, Functions, Container Apps
- **Security**: Managed Identity, Key Vault, Private Endpoints
- **Monitoring**: Application Insights, Azure Monitor

### 5. Join the Community

- **GitHub**: [Report issues or contribute](https://github.com/jeremy-schaab/Microsoft.Extensions.AI.Anthropic)
- **Microsoft.Extensions.AI Docs**: [Official documentation](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai)
- **Anthropic Docs**: [API reference](https://docs.anthropic.com/en/api)

## Quick Reference Card

### Essential Code Snippets

**Basic Chat**:
```csharp
var response = await chatClient.GetResponseAsync("Hello, Claude!");
Console.WriteLine(response.Text);
```

**Streaming**:
```csharp
await foreach (var update in chatClient.GetStreamingResponseAsync(messages))
    Console.Write(update.Text);
```

**With Options**:
```csharp
var options = new ChatOptions
{
    ModelId = "claude-sonnet-4-5",
    Temperature = 0.7f,
    MaxOutputTokens = 1024
};
var response = await chatClient.GetResponseAsync(messages, options);
```

**Dependency Injection**:
```csharp
builder.Services.AddAnthropicFoundryChatClientFromEnvironment(
    modelId: "claude-sonnet-4-5");
```

### Environment Variables

**Azure Foundry**:
```bash
ANTHROPIC_FOUNDRY_RESOURCE=your-resource-name
ANTHROPIC_FOUNDRY_API_KEY=your-api-key
```

**Standard API**:
```bash
ANTHROPIC_API_KEY=sk-ant-api03-...
```

### Common Model IDs

- `claude-sonnet-4-5` - Balanced (recommended)
- `claude-opus-4` - Best quality
- `claude-haiku-4` - Fastest

---

**Happy coding with Claude!** If you run into issues, check the [Troubleshooting](#troubleshooting) section or consult the [API Reference](API-REFERENCE.md).
