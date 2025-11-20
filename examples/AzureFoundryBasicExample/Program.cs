using Anthropic.Foundry;
using AzureFoundryBasicExample;
using DotNetEnv;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Anthropic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


// Load environment variables from .env file if it exists
Env.Load();

// This example demonstrates basic usage of the Anthropic chat client
// with Azure Anthropic Foundry (Azure-hosted Anthropic API)

var builder = Host.CreateApplicationBuilder(args);

// ========================================
// Authentication Method 1: Environment Variables (Recommended for Development)
// ========================================
// Reads configuration from environment variables:
// - ANTHROPIC_FOUNDRY_RESOURCE (required)
// - ANTHROPIC_FOUNDRY_API_KEY (optional, uses Azure Identity if not set)
builder.Services.AddAnthropicFoundryChatClientFromEnvironment(
    resourceName: null, // Will read from ANTHROPIC_FOUNDRY_RESOURCE env var
    modelId: "claude-sonnet-4-5");

// ========================================
// Authentication Method 2: Explicit API Key (Development/Testing)
// ========================================
// Uncomment to use explicit resource name and API key:
/*
builder.Services.AddAnthropicFoundryChatClient(
    resourceName: "my-anthropic-resource",
    apiKey: "sk-ant-foundry-xxxxx",
    modelId: "claude-sonnet-4-5");
*/

// ========================================
// Authentication Method 3: Azure Identity (Production - RECOMMENDED)
// ========================================
// Uncomment to use Azure Identity (DefaultAzureCredential):
// This is the MOST SECURE method for production deployments
/*
using Anthropic.Foundry;
using Azure.Identity;

var credentials = new AnthropicFoundryIdentityTokenCredentials(
    tokenCredential: new DefaultAzureCredential(),
    resourceName: "my-anthropic-resource");

builder.Services.AddAnthropicFoundryChatClient(
    credentials: credentials,
    modelId: "claude-sonnet-4-5");
*/

// ========================================
// Authentication Method 4: Bearer Token (Advanced)
// ========================================
// Uncomment to use bearer token authentication:
/*
using Anthropic.Foundry;

var credentials = new AnthropicFoundryBearerTokenCredentials(
    bearerToken: "your-bearer-token",
    resourceName: "my-anthropic-resource");

builder.Services.AddAnthropicFoundryChatClient(
    credentials: credentials,
    modelId: "claude-sonnet-4-5");
*/

// ========================================
// Alternative: IChatClientBuilder Pattern (Middleware Support)
// ========================================
// Uncomment to use builder pattern with middleware:
/*
builder.Services.AddChatClient(chatBuilder => chatBuilder
    .UseAnthropicFoundryFromEnvironment(modelId: "claude-sonnet-4-5")
    // .UseLogging()        // Add logging middleware
    // .UseOpenTelemetry()  // Add telemetry middleware
    );
*/

var host = builder.Build();

// Get the chat client from DI
var chatClient = host.Services.GetRequiredService<IChatClient>();

Console.WriteLine("Microsoft.Extensions.AI.Anthropic - Azure Foundry Example");
Console.WriteLine("===========================================================");
Console.WriteLine();

// Check if required environment variables are configured
var resourceName = Environment.GetEnvironmentVariable("ANTHROPIC_FOUNDRY_RESOURCE");
var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_FOUNDRY_API_KEY");

if (string.IsNullOrWhiteSpace(resourceName))
{
    Console.WriteLine("ERROR: ANTHROPIC_FOUNDRY_RESOURCE environment variable is not set.");
    Console.WriteLine();
    Console.WriteLine("Required environment variables:");
    Console.WriteLine("  ANTHROPIC_FOUNDRY_RESOURCE - Your Azure resource name (required)");
    Console.WriteLine("  ANTHROPIC_FOUNDRY_API_KEY - Your API key (optional, uses Azure Identity if not set)");
    Console.WriteLine();
    Console.WriteLine("Example:");
    Console.WriteLine("  Windows:");
    Console.WriteLine("    set ANTHROPIC_FOUNDRY_RESOURCE=my-resource-name");
    Console.WriteLine("    set ANTHROPIC_FOUNDRY_API_KEY=your-api-key-here");
    Console.WriteLine();
    Console.WriteLine("  Linux/Mac:");
    Console.WriteLine("    export ANTHROPIC_FOUNDRY_RESOURCE=my-resource-name");
    Console.WriteLine("    export ANTHROPIC_FOUNDRY_API_KEY=your-api-key-here");
    return;
}

Console.WriteLine($"Resource: {resourceName} ✓");
Console.WriteLine($"Authentication: {(string.IsNullOrWhiteSpace(apiKey) ? "Azure Identity (DefaultAzureCredential)" : "API Key")} ✓");
Console.WriteLine("Model: claude-sonnet-4-5");
Console.WriteLine();

try
{
    // Example 1: Simple chat completion
    Console.WriteLine("Example 1: Simple Chat Completion");
    Console.WriteLine("----------------------------------");

    var messages = new List<ChatMessage>
    {
        new(ChatRole.User, "What are the main benefits of using Azure for hosting AI services? Please answer in 2-3 sentences.")
    };

    Console.WriteLine("Sending request to Azure Anthropic Foundry...");
    var response = await chatClient.GetResponseAsync(messages);

    Console.WriteLine($"Response: {response.Text}");
    Console.WriteLine($"Finish Reason: {response.FinishReason}");
    Console.WriteLine($"Model: {response.ModelId}");
    Console.WriteLine();

    // Example 2: Chat with system message
    Console.WriteLine("Example 2: Chat with System Message");
    Console.WriteLine("------------------------------------");

    var messagesWithSystem = new List<ChatMessage>
    {
        new(ChatRole.System, "You are a helpful Azure cloud architect assistant. Provide concise, technical responses."),
        new(ChatRole.User, "What is the difference between Azure App Service and Azure Container Apps?")
    };

    Console.WriteLine("User: What is the difference between Azure App Service and Azure Container Apps?");
    var response2 = await chatClient.GetResponseAsync(messagesWithSystem);
    Console.WriteLine($"Assistant: {response2.Text}");
    Console.WriteLine();

    // Example 3: Streaming response
    Console.WriteLine("Example 3: Streaming Response");
    Console.WriteLine("------------------------------");

    var streamMessages = new List<ChatMessage>
    {
        new(ChatRole.User, "List 3 Azure services commonly used with AI applications, one per line.")
    };

    Console.WriteLine("Streaming response:");
    await foreach (var update in chatClient.GetStreamingResponseAsync(streamMessages))
    {
        if (update.Text is not null)
        {
            Console.Write(update.Text);
        }
    }
    Console.WriteLine();
    Console.WriteLine();

    // Example 4: Usage tracking
    Console.WriteLine("Example 4: Usage Tracking");
    Console.WriteLine("-------------------------");

    var usageMessages = new List<ChatMessage>
    {
        new(ChatRole.User, "Hello!")
    };

    var response4 = await chatClient.GetResponseAsync(usageMessages);

    Console.WriteLine($"Response: {response4.Text}");

    // Extract usage information
    var usageContent = response4.Messages[0].Contents.OfType<UsageContent>().FirstOrDefault();
    if (usageContent is not null)
    {
        Console.WriteLine($"Input Tokens: {usageContent.Details.InputTokenCount}");
        Console.WriteLine($"Output Tokens: {usageContent.Details.OutputTokenCount}");
        Console.WriteLine($"Total Tokens: {usageContent.Details.TotalTokenCount}");
    }
    Console.WriteLine();

    Console.WriteLine("Examples completed successfully! ✓");
}
catch (Exception ex)
{
    Console.WriteLine($"ERROR: {ex.Message}");
    Console.WriteLine();
    Console.WriteLine("Stack Trace:");
    Console.WriteLine(ex.StackTrace);
}
