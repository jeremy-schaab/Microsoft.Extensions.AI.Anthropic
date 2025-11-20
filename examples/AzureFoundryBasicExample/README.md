# Azure Foundry Basic Example

This example demonstrates how to use **Microsoft.Extensions.AI.Anthropic** with **Azure Anthropic Foundry** - the Azure-hosted Anthropic API. Azure Foundry provides enterprise-grade features including:

- **Azure Authentication**: Managed Identity, DefaultAzureCredential (no hardcoded API keys)
- **Enterprise Security**: Azure RBAC, private endpoints, virtual networks
- **Compliance**: Azure compliance certifications and data residency
- **Integration**: Seamless integration with Azure services

## Prerequisites

### 1. Azure Anthropic Foundry Resource

You need access to an Azure Anthropic Foundry resource. If you don't have one:

1. **Contact Azure Support**: Azure Anthropic Foundry is currently in private preview
2. **Get Resource Name**: Once provisioned, note your resource name (e.g., `my-anthropic-resource`)
3. **Get API Key** (optional): Found in Azure Portal under resource settings

### 2. .NET 9.0 SDK

Install the latest .NET 9.0 SDK:
```bash
dotnet --version  # Should be 9.0.x or higher
```

## Quick Start

### Option 1: Helper Scripts (Easiest)

Use the provided helper scripts for a guided setup:

**Windows (PowerShell)**:
```powershell
.\run.ps1 -ResourceName your-resource-name -ApiKey your-api-key

# Or use Azure Identity
.\run.ps1 -ResourceName your-resource-name -UseAzureIdentity
```

**Linux/macOS (Bash)**:
```bash
chmod +x run.sh
./run.sh -r your-resource-name -k your-api-key

# Or use Azure Identity
./run.sh -r your-resource-name --azure-identity
```

### Option 2: Environment Variables (Recommended)

Set the following environment variables:

**Windows (Command Prompt)**:
```cmd
set ANTHROPIC_FOUNDRY_RESOURCE=your-resource-name
set ANTHROPIC_FOUNDRY_API_KEY=your-api-key-here
```

**Windows (PowerShell)**:
```powershell
$env:ANTHROPIC_FOUNDRY_RESOURCE="your-resource-name"
$env:ANTHROPIC_FOUNDRY_API_KEY="your-api-key-here"
```

**Linux/macOS (Bash/Zsh)**:
```bash
export ANTHROPIC_FOUNDRY_RESOURCE=your-resource-name
export ANTHROPIC_FOUNDRY_API_KEY=your-api-key-here
```

Then run the example:
```bash
dotnet run
```

### Option 3: .env File (Convenient)

1. **Create .env file**:
   ```bash
   # Windows
   copy .env.example .env

   # Linux/macOS
   cp .env.example .env
   ```

2. **Edit .env** and fill in your credentials:
   ```bash
   ANTHROPIC_FOUNDRY_RESOURCE=your-resource-name
   ANTHROPIC_FOUNDRY_API_KEY=your-api-key
   ```

3. **Run with helper scripts** (they auto-load .env):
   ```bash
   # Windows
   .\run.ps1

   # Linux/macOS
   ./run.sh
   ```

### Option 4: Azure Identity (Production)

For production environments, use **Azure Identity** instead of API keys:

1. **Remove API Key**: Don't set `ANTHROPIC_FOUNDRY_API_KEY`
2. **Set Resource Name Only**:
   ```bash
   export ANTHROPIC_FOUNDRY_RESOURCE=your-resource-name
   ```
3. **Configure Azure Authentication**:
   - **Local Development**: `az login` (Azure CLI)
   - **Azure App Service**: Enable Managed Identity
   - **Azure Functions**: Enable Managed Identity
   - **Azure Container Apps**: Enable Managed Identity

The example will automatically use `DefaultAzureCredential` when no API key is provided.

## Authentication Methods

Azure Foundry supports **three authentication methods**:

### 1. API Key Authentication

**Use Case**: Development, testing, quick prototyping

**Setup**:
```bash
export ANTHROPIC_FOUNDRY_RESOURCE=my-resource
export ANTHROPIC_FOUNDRY_API_KEY=sk-ant-foundry-xxxxx
```

**Code**:
```csharp
builder.Services.AddAnthropicFoundryChatClient(
    resourceName: "my-resource",
    apiKey: "sk-ant-foundry-xxxxx",
    modelId: "claude-sonnet-4-5");
```

**Pros**: Simple, fast to set up
**Cons**: Less secure, requires key rotation, not recommended for production

---

### 2. Azure Identity (DefaultAzureCredential)

**Use Case**: Production, enterprise environments, CI/CD pipelines

**Setup**:
```bash
# Local development
az login

# Azure App Service / Functions / Container Apps
# Enable Managed Identity in Azure Portal
```

**Code**:
```csharp
// Automatically uses DefaultAzureCredential when API key not set
builder.Services.AddAnthropicFoundryChatClientFromEnvironment(
    modelId: "claude-sonnet-4-5");
```

**Credential Chain** (tried in order):
1. **Environment variables** (`AZURE_CLIENT_ID`, `AZURE_CLIENT_SECRET`, etc.)
2. **Managed Identity** (Azure App Service, Functions, VMs, Container Apps)
3. **Visual Studio** (VS Code, Visual Studio)
4. **Azure CLI** (`az login`)
5. **Azure PowerShell**

**Pros**: No secrets in code, automatic credential rotation, Azure RBAC
**Cons**: Requires Azure setup, more complex initial configuration

---

### 3. Bearer Token Authentication

**Use Case**: Custom token providers, advanced scenarios

**Setup**:
```bash
export ANTHROPIC_FOUNDRY_RESOURCE=my-resource
export ANTHROPIC_FOUNDRY_BEARER_TOKEN=your-bearer-token
```

**Code**:
```csharp
var credentials = new AnthropicFoundryBearerTokenCredentials(
    bearerToken: "your-bearer-token",
    resourceName: "my-resource");

builder.Services.AddAnthropicFoundryChatClient(
    credentials: credentials,
    modelId: "claude-sonnet-4-5");
```

**Pros**: Flexible token management
**Cons**: Manual token refresh required

---

## Configuration Examples

### Example 1: Environment Variables (FromEnv)

The simplest approach - reads all configuration from environment:

```csharp
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Anthropic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// Reads ANTHROPIC_FOUNDRY_RESOURCE and ANTHROPIC_FOUNDRY_API_KEY
builder.Services.AddAnthropicFoundryChatClientFromEnvironment(
    modelId: "claude-sonnet-4-5");

var host = builder.Build();
var chatClient = host.Services.GetRequiredService<IChatClient>();

var response = await chatClient.GetResponseAsync("Hello!");
Console.WriteLine(response.Text);
```

### Example 2: Explicit Resource Name

Specify resource name in code, read API key from environment:

```csharp
builder.Services.AddAnthropicFoundryChatClient(
    resourceName: "my-anthropic-resource",
    apiKey: Environment.GetEnvironmentVariable("ANTHROPIC_FOUNDRY_API_KEY")!,
    modelId: "claude-sonnet-4-5");
```

### Example 3: Azure Identity (Managed Identity)

Production-ready authentication without API keys:

```csharp
using Anthropic.Foundry;
using Azure.Identity;

// Create credentials using Azure Identity
var credentials = new AnthropicFoundryIdentityTokenCredentials(
    tokenCredential: new DefaultAzureCredential(),
    resourceName: "my-anthropic-resource");

builder.Services.AddAnthropicFoundryChatClient(
    credentials: credentials,
    modelId: "claude-sonnet-4-5");
```

### Example 4: Factory Pattern

Advanced scenario - custom client creation logic:

```csharp
builder.Services.AddAnthropicFoundryChatClient(
    clientFactory: sp =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        var resourceName = config["Azure:Anthropic:ResourceName"];

        var credentials = new AnthropicFoundryApiKeyCredentials(
            apiKey: config["Azure:Anthropic:ApiKey"],
            resourceName: resourceName);

        return new AnthropicFoundryClient(credentials);
    },
    modelId: "claude-sonnet-4-5");
```

### Example 5: IChatClientBuilder (Middleware Pipeline)

Use the builder pattern to add middleware:

```csharp
builder.Services.AddChatClient(builder => builder
    .UseAnthropicFoundryFromEnvironment(modelId: "claude-sonnet-4-5")
    .UseLogging()           // Add logging
    .UseOpenTelemetry()     // Add telemetry
    .UseRetryPolicy());     // Add retry logic
```

## Running the Example

### 1. Clone the Repository
```bash
cd C:\Users\jschaab\source\repos\Research\Microsoft.Extensions.AI.Anthropic
```

### 2. Set Environment Variables
```bash
# Windows
set ANTHROPIC_FOUNDRY_RESOURCE=your-resource-name
set ANTHROPIC_FOUNDRY_API_KEY=your-api-key

# Linux/macOS
export ANTHROPIC_FOUNDRY_RESOURCE=your-resource-name
export ANTHROPIC_FOUNDRY_API_KEY=your-api-key
```

### 3. Run the Example
```bash
cd examples/AzureFoundryBasicExample
dotnet run
```

### Expected Output
```
Microsoft.Extensions.AI.Anthropic - Azure Foundry Example
===========================================================

Resource: my-resource ✓
Authentication: API Key ✓
Model: claude-sonnet-4-5

Example 1: Simple Chat Completion
----------------------------------
Sending request to Azure Anthropic Foundry...
Response: Azure offers several key benefits for hosting AI services: enterprise-grade security and compliance, global scalability with high availability...
Finish Reason: Stop
Model: claude-sonnet-4-5

Example 2: Chat with System Message
------------------------------------
User: What is the difference between Azure App Service and Azure Container Apps?
Assistant: Azure App Service is a fully managed PaaS for hosting web apps...

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

## What the Example Demonstrates

1. **Simple Chat Completion**: Basic question-answer with Claude
2. **System Messages**: Using system prompts to guide Claude's behavior
3. **Streaming Responses**: Real-time token streaming for better UX
4. **Usage Tracking**: Monitor token consumption for cost management

## Model IDs

Common Claude models available on Azure Foundry:

| Model ID | Description | Context Window | Best For |
|----------|-------------|----------------|----------|
| `claude-sonnet-4-5` | Claude Sonnet 4.5 | 200K tokens | Balanced performance |
| `claude-opus-4` | Claude Opus 4 | 200K tokens | Complex reasoning |
| `claude-haiku-3-5` | Claude Haiku 3.5 | 200K tokens | Fast, cost-effective |

## Advanced Features

### Extended Thinking (Claude Opus 4)

Enable Claude's extended thinking mode for complex reasoning:

```csharp
var options = new ChatOptions
{
    AdditionalProperties = new Dictionary<string, object>
    {
        ["thinking"] = new { type = "enabled", budget_tokens = 10000 }
    }
};

var response = await chatClient.GetResponseAsync(messages, options);
```

### Function Calling / Tool Use

Integrate tools with Claude:

```csharp
var tools = new List<AITool>
{
    AIFunction.Create((string location) => GetWeather(location), "get_weather")
};

var options = new ChatOptions
{
    Tools = tools,
    ToolMode = ChatToolMode.Auto
};

var response = await chatClient.GetResponseAsync(messages, options);
```

### Multi-Modal (Vision)

Send images to Claude:

```csharp
var imageBytes = File.ReadAllBytes("diagram.png");
var imageContent = new DataContent(imageBytes, "image/png");

var messages = new List<ChatMessage>
{
    new(ChatRole.User, [
        new TextContent("What's in this image?"),
        imageContent
    ])
};

var response = await chatClient.GetResponseAsync(messages);
```

## Troubleshooting

### Error: "ANTHROPIC_FOUNDRY_RESOURCE environment variable is not set"

**Solution**: Set the environment variable:
```bash
export ANTHROPIC_FOUNDRY_RESOURCE=your-resource-name
```

### Error: "Failed to create Azure Foundry credentials from environment"

**Cause**: Missing or invalid environment variables

**Solution**: Verify environment variables are set correctly:
```bash
# Check variables
echo $ANTHROPIC_FOUNDRY_RESOURCE
echo $ANTHROPIC_FOUNDRY_API_KEY

# Set if missing
export ANTHROPIC_FOUNDRY_RESOURCE=your-resource-name
export ANTHROPIC_FOUNDRY_API_KEY=your-api-key
```

### Error: "Authentication failed" (Azure Identity)

**Cause**: Azure Identity credential chain failed

**Solutions**:
1. **Local Development**: Run `az login`
2. **Azure App Service**: Enable Managed Identity in Azure Portal
3. **Check Permissions**: Ensure identity has required RBAC roles
4. **Fallback to API Key**: Set `ANTHROPIC_FOUNDRY_API_KEY` temporarily

### Error: "The model 'claude-sonnet-4-5' is not supported"

**Cause**: Model not available in your region or subscription

**Solution**: Check available models in Azure Portal or try:
- `claude-opus-4`
- `claude-haiku-3-5`

## Azure Deployment

### Azure App Service

1. **Enable Managed Identity**:
   ```bash
   az webapp identity assign --name my-app --resource-group my-rg
   ```

2. **Set Environment Variables**:
   ```bash
   az webapp config appsettings set --name my-app --resource-group my-rg \
     --settings ANTHROPIC_FOUNDRY_RESOURCE=my-resource
   ```

3. **Grant RBAC Permissions**:
   - Assign "Cognitive Services User" role to Managed Identity

### Azure Functions

1. **Enable Managed Identity** in Azure Portal

2. **Add App Settings**:
   - `ANTHROPIC_FOUNDRY_RESOURCE`: Your resource name

3. **Deploy Function**:
   ```bash
   func azure functionapp publish my-function-app
   ```

### Azure Container Apps

1. **Enable Managed Identity**:
   ```bash
   az containerapp identity assign --name my-app --resource-group my-rg
   ```

2. **Set Environment Variables**:
   ```bash
   az containerapp update --name my-app --resource-group my-rg \
     --set-env-vars ANTHROPIC_FOUNDRY_RESOURCE=my-resource
   ```

## Security Best Practices

1. **Never Commit API Keys**: Use `.gitignore` for `.env` files
2. **Use Azure Identity**: Prefer Managed Identity over API keys in production
3. **Rotate Keys Regularly**: If using API keys, rotate every 90 days
4. **Use Azure Key Vault**: Store API keys in Key Vault, reference in App Settings
5. **Enable Private Endpoints**: Restrict network access to Azure Foundry resource
6. **Monitor Usage**: Set up Azure Monitor alerts for unusual API usage

## Cost Management

Monitor token usage to control costs:

```csharp
var response = await chatClient.GetResponseAsync(messages);

var usage = response.Messages[0].Contents.OfType<UsageContent>().FirstOrDefault();
if (usage is not null)
{
    Console.WriteLine($"Input: {usage.Details.InputTokenCount} tokens");
    Console.WriteLine($"Output: {usage.Details.OutputTokenCount} tokens");
    Console.WriteLine($"Total: {usage.Details.TotalTokenCount} tokens");
}
```

**Cost Optimization Tips**:
- Use `claude-haiku-3-5` for simple tasks (most cost-effective)
- Use `claude-sonnet-4-5` for balanced workloads
- Reserve `claude-opus-4` for complex reasoning
- Implement prompt caching for repeated content
- Set `max_tokens` to prevent runaway costs

## Additional Resources

- **Project Documentation**: `../../README.md`
- **Research Plan**: `../../docs/research/anthropic-integration-research-plan.md`
- **Anthropic API Docs**: https://docs.anthropic.com/en/api
- **Azure Foundry Docs**: https://learn.microsoft.com/azure/ai-services/anthropic
- **Microsoft.Extensions.AI**: https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai

## Support

For issues specific to this example:
- **GitHub Issues**: https://github.com/jeremy-schaab/Microsoft.Extensions.AI.Anthropic/issues

For Azure Foundry issues:
- **Azure Support**: https://portal.azure.com/#blade/Microsoft_Azure_Support/HelpAndSupportBlade
