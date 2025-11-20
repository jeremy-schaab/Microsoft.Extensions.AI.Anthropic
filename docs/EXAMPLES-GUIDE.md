# Examples Guide - Microsoft.Extensions.AI.Anthropic

**Version**: 0.3.1-preview
**Last Updated**: 2025-01-19
**Repository Location**: `examples/`

This guide provides an overview of all example projects included with Microsoft.Extensions.AI.Anthropic, how to run them, and what each demonstrates.

## Table of Contents

- [Overview](#overview)
- [Prerequisites](#prerequisites)
- [Example Projects](#example-projects)
  - [1. AzureFoundryBasicExample](#1-azurefoundrybasicexample)
  - [2. StreamingChatExample](#2-streamingchatexample)
  - [3. ToolCallingExample](#3-toolcallingexample)
  - [4. VisionExample](#4-visionexample)
  - [5. AzureFoundryManagedIdentityExample](#5-azurefoundrymanagedidentityexample)
- [Quick Start Matrix](#quick-start-matrix)
- [Common Setup Steps](#common-setup-steps)
- [Troubleshooting](#troubleshooting)

## Overview

All example projects are located in the `examples/` directory and demonstrate real-world usage patterns for the library. Each example is a complete, runnable console application with comprehensive documentation.

### Example Directory Structure

```
examples/
├── AzureFoundryBasicExample/           # Azure Foundry fundamentals
├── StreamingChatExample/               # Real-time streaming
├── ToolCallingExample/                 # Function calling / tools
├── VisionExample/                      # Image and PDF analysis
└── AzureFoundryManagedIdentityExample/ # Production authentication
```

## Prerequisites

### All Examples Require

1. **.NET 9.0 SDK**
   ```bash
   dotnet --version  # Should be 9.0.x or higher
   ```

2. **API Access** (one of):
   - Azure Anthropic Foundry resource (recommended)
   - Standard Anthropic API account

3. **Development Environment**:
   - Visual Studio 2022, VS Code, Rider, or command line

### Azure Foundry Examples Require

- Azure subscription
- Anthropic Foundry resource provisioned
- Resource name and API key OR Managed Identity

### Standard API Examples Require

- Anthropic API key from https://console.anthropic.com/

## Example Projects

### 1. AzureFoundryBasicExample

**Location**: `examples/AzureFoundryBasicExample/`

**What It Demonstrates**:
- Azure Foundry authentication (3 methods)
- Basic chat completion
- System message handling
- Streaming responses
- Usage tracking
- Environment variable configuration
- Dependency injection setup

**Authentication Methods Shown**:
1. Environment variables (API key)
2. Explicit API key
3. Azure Identity (DefaultAzureCredential)
4. Bearer token

**Files**:
- `Program.cs` - Main application (7.4 KB)
- `README.md` - Comprehensive setup guide (15 KB)
- `.env.example` - Environment variable template
- `run.ps1` - PowerShell helper script
- `run.sh` - Bash helper script

#### Quick Start

**Windows (PowerShell)**:
```powershell
cd examples\AzureFoundryBasicExample
.\run.ps1 -ResourceName your-resource-name -ApiKey your-api-key
```

**Linux/macOS**:
```bash
cd examples/AzureFoundryBasicExample
chmod +x run.sh
./run.sh -r your-resource-name -k your-api-key
```

**Manual Setup**:
```bash
# Set environment variables
export ANTHROPIC_FOUNDRY_RESOURCE=your-resource-name
export ANTHROPIC_FOUNDRY_API_KEY=your-api-key

# Run
dotnet run
```

#### Expected Output

```
Microsoft.Extensions.AI.Anthropic - Azure Foundry Example
===========================================================

Resource: my-resource ✓
Authentication: API Key ✓
Model: claude-sonnet-4-5

Example 1: Simple Chat Completion
----------------------------------
Sending request to Azure Anthropic Foundry...
Response: Azure offers several key benefits for hosting AI services: enterprise-grade security...
Finish Reason: Stop
Model: claude-sonnet-4-5

Example 2: Chat with System Message
------------------------------------
User: What is the difference between Azure App Service and Azure Container Apps?
Assistant: Azure App Service is a fully managed PaaS...

Example 3: Streaming Response
------------------------------
Streaming response:
1. Azure OpenAI Service
2. Azure Machine Learning
3. Azure Cognitive Services

Example 4: Usage Tracking
-------------------------
Response: Hello! How can I assist you today?
Input Tokens: 12
Output Tokens: 9
Total Tokens: 21

Examples completed successfully! ✓
```

#### Key Code Snippets

**Environment-based Authentication**:
```csharp
builder.Services.AddAnthropicFoundryChatClientFromEnvironment(
    modelId: "claude-sonnet-4-5");
```

**Azure Identity (Managed Identity)**:
```csharp
var credentials = new AnthropicFoundryIdentityTokenCredentials(
    tokenCredential: new DefaultAzureCredential(),
    resourceName: "my-anthropic-resource");

builder.Services.AddAnthropicFoundryChatClient(
    credentials: credentials,
    modelId: "claude-sonnet-4-5");
```

---

### 2. StreamingChatExample

**Location**: `examples/StreamingChatExample/`

**What It Demonstrates**:
- Real-time token streaming
- Multi-turn conversation with history
- Cancellation handling (Ctrl+C)
- Usage statistics display
- Color-coded console output
- Thread-safe streaming (C# 13's `Lock` type)
- Dual API support (Azure Foundry + Standard)

**Files**:
- `Program.cs` - Interactive streaming chat (11 KB)
- `README.md` - Usage instructions
- `StreamingChatExample.csproj` - Project file

#### Quick Start

**Azure Foundry**:
```bash
export ANTHROPIC_FOUNDRY_RESOURCE=your-resource-name
export ANTHROPIC_FOUNDRY_API_KEY=your-api-key

cd examples/StreamingChatExample
dotnet run
```

**Standard API**:
```bash
export ANTHROPIC_API_KEY=sk-ant-api03-...

cd examples/StreamingChatExample
dotnet run
```

#### Expected Output

```
Streaming Chat Example - Microsoft.Extensions.AI.Anthropic
===========================================================
Type 'exit' to quit, Ctrl+C to cancel streaming

Authentication: Azure Foundry (API Key)
Model: claude-sonnet-4-5

You: Write me a short poem about coding.

Claude:
Lines of code dance on the screen,
Logic flows where bugs convene,
Brackets close and functions call,
Building systems, great and small.

[Finished - Tokens: 45 | Duration: 2.3s]

You: Make it shorter.

Claude:
Code flows, bugs grow,
Functions call, systems all.

[Finished - Tokens: 23 | Duration: 1.1s]

You: exit

Total conversation tokens: 68
Goodbye!
```

#### Key Code Snippets

**Streaming Response**:
```csharp
Console.Write("Claude: ");
await foreach (var update in chatClient.GetStreamingResponseAsync(messages))
{
    if (update.Text != null)
    {
        Console.Write(update.Text);
    }

    if (update.FinishReason != null)
    {
        Console.WriteLine($"\n[Finished - Tokens: {totalTokens}]");
    }
}
```

**Cancellation Handling**:
```csharp
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (s, e) =>
{
    e.Cancel = true;
    cts.Cancel();
    Console.WriteLine("\n[Cancelled]");
};

await foreach (var update in chatClient.GetStreamingResponseAsync(messages, cancellationToken: cts.Token))
{
    // Process updates
}
```

---

### 3. ToolCallingExample

**Location**: `examples/ToolCallingExample/`

**What It Demonstrates**:
- Function/tool calling basics
- Multiple tool definitions
- Tool choice modes (Auto, RequireAny, RequireSpecific)
- Multi-turn conversations with tools
- Error handling in tools
- JSON serialization for complex arguments/results

**Tools Implemented**:
1. `get_weather` - Weather lookup
2. `calculate` - Mathematical calculations
3. `get_current_time` - Time zone conversions

**Files**:
- `Program.cs` - 4 complete tool calling examples (540 lines)
- `README.md` - Comprehensive tool calling guide
- `ToolCallingExample.csproj` - Project file

#### Quick Start

```bash
export ANTHROPIC_FOUNDRY_RESOURCE=your-resource-name
export ANTHROPIC_FOUNDRY_API_KEY=your-api-key

cd examples/ToolCallingExample
dotnet run
```

#### Expected Output

```
Tool Calling Example - Microsoft.Extensions.AI.Anthropic
=========================================================

Example 1: Single Tool Call
----------------------------
User: What's the weather in San Francisco?

[Tool Call] get_weather
  Arguments: { "location": "San Francisco" }
  Result: Sunny, 72°F

Claude: The weather in San Francisco is currently sunny and 72°F.


Example 2: Multiple Tools in One Turn
--------------------------------------
User: What's the weather in Seattle and what time is it there?

[Tool Call] get_weather
  Arguments: { "location": "Seattle" }
  Result: Cloudy, 58°F

[Tool Call] get_current_time
  Arguments: { "timezone": "America/Los_Angeles" }
  Result: 2025-01-19 14:23:15 PST

Claude: In Seattle, the weather is cloudy and 58°F. The current time there is 2:23 PM PST.


Example 3: Calculator Tool
---------------------------
User: Calculate (15 * 8) + 42

[Tool Call] calculate
  Arguments: { "expression": "(15 * 8) + 42" }
  Result: 162

Claude: The result of (15 * 8) + 42 is 162.


Example 4: Multi-turn with Tools
---------------------------------
[Turn 1]
User: What's the weather in Tokyo?
Claude: (uses get_weather) The weather in Tokyo is rainy and 65°F.

[Turn 2]
User: And what time is it there?
Claude: (uses get_current_time) It's currently 7:23 AM JST in Tokyo.
```

#### Key Code Snippets

**Define a Tool**:
```csharp
var weatherTool = AIFunctionFactory.Create(
    (string location) =>
    {
        // Mock weather data
        return $"Weather in {location}: Sunny, 72°F";
    },
    name: "get_weather",
    description: "Get the current weather for a specific location");
```

**Use Tools with Auto Mode**:
```csharp
var options = new ChatOptions
{
    ModelId = "claude-sonnet-4-5",
    Tools = [weatherTool, timeTool, calculatorTool],
    ToolMode = AutoChatToolMode.Instance  // Claude decides when to use tools
};

var response = await chatClient.GetResponseAsync(messages, options);
```

**Process Tool Calls**:
```csharp
foreach (var toolCall in response.Message.Contents.OfType<FunctionCallContent>())
{
    Console.WriteLine($"[Tool Call] {toolCall.Name}");
    Console.WriteLine($"  Arguments: {JsonSerializer.Serialize(toolCall.Arguments)}");

    // Execute tool (automatically handled by framework)
    // Add result to conversation
    messages.Add(new ChatMessage(ChatRole.Tool,
        new FunctionResultContent(toolCall.CallId, toolCall.Name, result)));
}
```

---

### 4. VisionExample

**Location**: `examples/VisionExample/`

**What It Demonstrates**:
- Image analysis from files (JPEG, PNG, GIF, WebP)
- Image analysis from URLs
- Multiple images in one message
- Text + image combined prompts
- PDF document analysis (Beta API)
- Streaming with vision
- Base64 encoding helpers
- MIME type detection

**Files**:
- `Program.cs` - 6 multi-modal scenarios
- `README.md` - Comprehensive vision guide (9 KB)
- `QUICK-START.md` - 5-minute getting started guide
- `SAMPLE-IMAGES.md` - Image setup instructions
- `VisionExample.csproj` - Project file

#### Quick Start

```bash
# 1. Set up environment
export ANTHROPIC_FOUNDRY_RESOURCE=your-resource-name
export ANTHROPIC_FOUNDRY_API_KEY=your-api-key

# 2. Add sample images (optional)
cd examples/VisionExample
mkdir media
# Copy images to media/ folder

# 3. Run
dotnet run
```

#### Expected Output

```
Vision Example - Microsoft.Extensions.AI.Anthropic
===================================================

Example 1: Analyze Image from File
-----------------------------------
Loading image: media/sample.jpg
Analyzing image...

Claude: This image shows a flowchart diagram illustrating a software architecture.
The diagram contains several interconnected boxes representing different components:
1. Frontend (React/TypeScript)
2. API Gateway
3. Microservices (3 separate services)
4. Database (PostgreSQL)
...


Example 2: Analyze Image from URL
----------------------------------
Image URL: https://upload.wikimedia.org/wikipedia/commons/thumb/0/0a/Python.svg/240px-Python.svg.png
Analyzing...

Claude: This is the Python programming language logo. It features two intertwined snakes
forming a stylized letter "P" - one in blue and one in yellow. The design represents
Python's philosophy of simplicity and elegance.


Example 3: Multiple Images
---------------------------
Analyzing 3 images...

Claude: I can see three different programming language logos:
1. Python - Blue and yellow intertwined snakes
2. JavaScript - Yellow "JS" text
3. C# - Purple hexagon with "C#" text

All three are popular modern programming languages used for different purposes.


Example 4: Combined Text and Image
-----------------------------------
Question: What design patterns can you identify in this diagram?

Claude: Looking at the architecture diagram, I can identify several design patterns:

1. **Microservices Pattern** - The system is decomposed into independent services
2. **API Gateway Pattern** - Single entry point for all client requests
3. **Database per Service** - Each microservice has its own database
4. **Load Balancing** - Distributes requests across service instances
...


Example 5: PDF Document Analysis (Beta)
----------------------------------------
Loading PDF: media/whitepaper.pdf
Analyzing document...

Claude: This appears to be a technical whitepaper about cloud-native architecture.
The document is structured into the following sections:
1. Executive Summary
2. Introduction to Cloud-Native
3. Core Principles...
[Note: PDF analysis requires Claude Opus 4 and Beta API]


Example 6: Streaming with Vision
---------------------------------
Analyzing image with streaming...

Claude: This architectural diagram illustrates a modern microservices-based system.
[Streaming real-time as tokens arrive...]
```

#### Key Code Snippets

**Analyze Image from File**:
```csharp
var imageBytes = await File.ReadAllBytesAsync("diagram.png");

var message = new ChatMessage(ChatRole.User, [
    new TextContent("What is in this image?"),
    new DataContent(imageBytes, "image/png")
]);

var response = await chatClient.GetResponseAsync([message]);
Console.WriteLine(response.Text);
```

**Analyze Image from URL**:
```csharp
using var httpClient = new HttpClient();
var imageBytes = await httpClient.GetByteArrayAsync("https://example.com/image.jpg");

var message = new ChatMessage(ChatRole.User, [
    new TextContent("Describe this image."),
    new DataContent(imageBytes, "image/jpeg")
]);
```

**Multiple Images**:
```csharp
var image1 = new DataContent(await File.ReadAllBytesAsync("img1.png"), "image/png");
var image2 = new DataContent(await File.ReadAllBytesAsync("img2.jpg"), "image/jpeg");

var message = new ChatMessage(ChatRole.User, [
    new TextContent("Compare these two images."),
    image1,
    image2
]);
```

**PDF Analysis** (Beta):
```csharp
var pdfBytes = await File.ReadAllBytesAsync("document.pdf");

var message = new ChatMessage(ChatRole.User, [
    new TextContent("Summarize this document."),
    new DataContent(pdfBytes, "application/pdf")
]);

var options = new ChatOptions
{
    ModelId = "claude-opus-4",  // PDF requires Opus 4
    AdditionalProperties = new Dictionary<string, object?>
    {
        ["betas"] = new[] { "pdfs-2024-09-25" }  // Enable PDF beta
    }
};
```

---

### 5. AzureFoundryManagedIdentityExample

**Location**: `examples/AzureFoundryManagedIdentityExample/`

**What It Demonstrates**:
- DefaultAzureCredential configuration
- System-assigned managed identity
- User-assigned managed identity
- Local development setup (Azure CLI)
- Azure deployment patterns (App Service, Functions, AKS, Container Apps)
- RBAC role assignments
- Infrastructure as Code (Bicep templates)
- CI/CD pipeline integration
- Monitoring and diagnostics
- Production-ready authentication

**Files**:
- `Program.cs` - DefaultAzureCredential implementation (9.2 KB)
- `README.md` - Complete authentication guide (20 KB)
- `DEPLOYMENT.md` - Production deployment guides (25 KB)
- `QUICKSTART.md` - Quick setup guide (5.1 KB)
- `appsettings.json` - Configuration template
- `Dockerfile` - Container support
- `.dockerignore` - Container optimization

#### Quick Start

**Local Development** (Azure CLI):
```bash
# 1. Login to Azure
az login

# 2. Set environment variable
export ANTHROPIC_FOUNDRY_RESOURCE=your-resource-name

# 3. Run
cd examples/AzureFoundryManagedIdentityExample
dotnet run
```

**Local Development** (Service Principal):
```bash
# 1. Set environment variables
export AZURE_CLIENT_ID=your-client-id
export AZURE_CLIENT_SECRET=your-client-secret
export AZURE_TENANT_ID=your-tenant-id
export ANTHROPIC_FOUNDRY_RESOURCE=your-resource-name

# 2. Run
dotnet run
```

#### Azure App Service Deployment

```bash
# 1. Create App Service
az webapp create \
  --name my-app \
  --resource-group my-rg \
  --plan my-plan \
  --runtime "DOTNET|9.0"

# 2. Enable managed identity
az webapp identity assign --name my-app --resource-group my-rg

# 3. Get principal ID
principalId=$(az webapp identity show \
  --name my-app \
  --resource-group my-rg \
  --query principalId -o tsv)

# 4. Grant RBAC permissions
az role assignment create \
  --role "Cognitive Services User" \
  --assignee $principalId \
  --scope /subscriptions/{sub-id}/resourceGroups/{rg}/providers/Microsoft.CognitiveServices/accounts/{resource}

# 5. Set app setting
az webapp config appsettings set \
  --name my-app \
  --resource-group my-rg \
  --settings ANTHROPIC_FOUNDRY_RESOURCE=your-resource

# 6. Deploy
az webapp deployment source config-zip \
  --name my-app \
  --resource-group my-rg \
  --src publish.zip
```

#### Expected Output

```
Managed Identity Example - Microsoft.Extensions.AI.Anthropic
=============================================================

Configuration:
  Resource: my-anthropic-resource
  Authentication: DefaultAzureCredential
  Credential Chain:
    1. EnvironmentCredential
    2. ManagedIdentityCredential
    3. VisualStudioCredential
    4. AzureCliCredential ✓ (Active)

Attempting authentication...
✓ Successfully authenticated using Azure CLI

Testing chat completion...
User: What are the benefits of using Managed Identity?

Claude: Managed Identity in Azure offers several key benefits:

1. **No Credential Management**: Eliminates the need to store and manage API keys
2. **Automatic Rotation**: Azure automatically rotates credentials
3. **RBAC Integration**: Fine-grained access control with Azure role assignments
4. **Audit Trail**: All access is logged for compliance and security
5. **Reduced Attack Surface**: No secrets in code, configuration, or environment variables

This approach is recommended for all production deployments on Azure.

✓ Example completed successfully!
```

#### Key Code Snippets

**DefaultAzureCredential Setup**:
```csharp
using Azure.Identity;
using Anthropic.Foundry;
using Microsoft.Extensions.AI.Anthropic;

var resourceName = Environment.GetEnvironmentVariable("ANTHROPIC_FOUNDRY_RESOURCE");

var credentials = new AnthropicFoundryIdentityTokenCredentials(
    tokenCredential: new DefaultAzureCredential(new DefaultAzureCredentialOptions
    {
        // Customize credential chain if needed
        ExcludeVisualStudioCredential = false,
        ExcludeAzureCliCredential = false,
        Diagnostics = { IsLoggingEnabled = true }
    }),
    resourceName: resourceName);

builder.Services.AddAnthropicFoundryChatClient(
    credentials: credentials,
    modelId: "claude-sonnet-4-5");
```

**Specific Credential Type**:
```csharp
// System-assigned managed identity
var credential = new ManagedIdentityCredential();

// User-assigned managed identity
var credential = new ManagedIdentityCredential(clientId: "your-client-id");

// Service principal
var credential = new ClientSecretCredential(
    tenantId: "your-tenant-id",
    clientId: "your-client-id",
    clientSecret: "your-client-secret");
```

---

## Quick Start Matrix

| Example | Authentication | Streaming | Tools | Vision | DI | Multi-turn | Difficulty |
|---------|----------------|-----------|-------|--------|-----|-----------|-----------|
| **AzureFoundryBasic** | ✅ 3 methods | ✅ | ❌ | ❌ | ✅ | ✅ | Beginner |
| **StreamingChat** | ✅ 2 methods | ✅ | ❌ | ❌ | ✅ | ✅ | Beginner |
| **ToolCalling** | ✅ | ❌ | ✅ 3 tools | ❌ | ✅ | ✅ | Intermediate |
| **Vision** | ✅ | ✅ | ❌ | ✅ Images+PDF | ✅ | ❌ | Intermediate |
| **ManagedIdentity** | ✅ Azure ID | ✅ | ❌ | ❌ | ✅ | ✅ | Advanced |

## Common Setup Steps

### 1. Clone Repository

```bash
git clone https://github.com/jeremy-schaab/Microsoft.Extensions.AI.Anthropic.git
cd Microsoft.Extensions.AI.Anthropic
```

### 2. Set Environment Variables

**Azure Foundry**:
```bash
export ANTHROPIC_FOUNDRY_RESOURCE=your-resource-name
export ANTHROPIC_FOUNDRY_API_KEY=your-api-key  # Optional if using Azure Identity
```

**Standard API**:
```bash
export ANTHROPIC_API_KEY=sk-ant-api03-...
```

### 3. Navigate to Example

```bash
cd examples/AzureFoundryBasicExample  # Or any other example
```

### 4. Run Example

```bash
dotnet run
```

### 5. Explore Code

Open `Program.cs` in your favorite editor to see the implementation.

## Troubleshooting

### Issue: "ANTHROPIC_FOUNDRY_RESOURCE environment variable is not set"

**Solution**:
```bash
export ANTHROPIC_FOUNDRY_RESOURCE=your-resource-name
```

### Issue: "Authentication failed" (401)

**Solutions**:
1. Check API key is correct
2. Verify resource name is correct
3. For Azure Identity: Run `az login`
4. Check RBAC permissions

### Issue: "The model 'xxx' is not supported"

**Solution**: Check available models for your deployment. Try:
- `claude-sonnet-4-5`
- `claude-opus-4`
- `claude-haiku-4`

### Issue: "Image file not found" (Vision example)

**Solution**:
```bash
cd examples/VisionExample
mkdir media
# Copy your images to media/ folder
# Or modify Program.cs to use your image paths
```

### Issue: Build errors

**Solution**:
```bash
# Clean and restore
dotnet clean
dotnet restore

# Rebuild
dotnet build
```

### Issue: PDF analysis not working

**Requirements**:
1. Use `claude-opus-4` model
2. Enable Beta API:
   ```csharp
   AdditionalProperties = new Dictionary<string, object?>
   {
       ["betas"] = new[] { "pdfs-2024-09-25" }
   }
   ```

### Issue: Helper scripts won't run (Linux/macOS)

**Solution**:
```bash
chmod +x run.sh
./run.sh -r your-resource -k your-api-key
```

---

## Next Steps

After running the examples:

1. **Modify Examples**: Change prompts, models, or parameters
2. **Combine Features**: Mix streaming with tools, vision with multi-turn
3. **Build Your Own**: Use examples as templates for your application
4. **Read Documentation**:
   - [Getting Started Guide](GETTING-STARTED.md)
   - [API Reference](API-REFERENCE.md)
   - [Authentication Guide](AUTHENTICATION-GUIDE.md)
   - [Architecture Documentation](ARCHITECTURE.md)

---

**Need Help?**
- GitHub Issues: https://github.com/jeremy-schaab/Microsoft.Extensions.AI.Anthropic/issues
- Microsoft.Extensions.AI Docs: https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai
- Anthropic API Docs: https://docs.anthropic.com/en/api
