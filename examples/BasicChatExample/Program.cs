using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Anthropic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// This example demonstrates basic usage of the Anthropic chat client
// with the standard Anthropic API (api.anthropic.com)

var builder = Host.CreateApplicationBuilder(args);

// Register the Anthropic chat client
// API key can be provided directly or via ANTHROPIC_API_KEY environment variable
builder.Services.AddAnthropicChatClient(
    apiKey: null, // Will read from ANTHROPIC_API_KEY env var
    modelId: "claude-sonnet-4-5");

var host = builder.Build();

// Get the chat client from DI
var chatClient = host.Services.GetRequiredService<IChatClient>();

Console.WriteLine("Microsoft.Extensions.AI.Anthropic - Basic Chat Example");
Console.WriteLine("========================================================");
Console.WriteLine();

// Check if API key is configured
var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.WriteLine("ERROR: ANTHROPIC_API_KEY environment variable is not set.");
    Console.WriteLine();
    Console.WriteLine("Please set your Anthropic API key:");
    Console.WriteLine("  Windows: set ANTHROPIC_API_KEY=your-api-key-here");
    Console.WriteLine("  Linux/Mac: export ANTHROPIC_API_KEY=your-api-key-here");
    return;
}

Console.WriteLine("API Key: Configured ✓");
Console.WriteLine("Model: claude-sonnet-4-5");
Console.WriteLine();

try
{
    // Example 1: Simple chat completion
    Console.WriteLine("Example 1: Simple Chat Completion");
    Console.WriteLine("----------------------------------");

    var messages = new List<ChatMessage>
    {
        new(ChatRole.User, "What is the capital of France? Please answer in one sentence.")
    };

    Console.WriteLine("Sending request to Claude...");
    var response = await chatClient.GetResponseAsync(messages);

    Console.WriteLine($"Response: {response.Text}");
    Console.WriteLine($"Finish Reason: {response.FinishReason}");
    Console.WriteLine($"Model: {response.ModelId}");
    Console.WriteLine();

    // Example 2: Multi-turn conversation
    Console.WriteLine("Example 2: Multi-Turn Conversation");
    Console.WriteLine("-----------------------------------");

    var conversation = new List<ChatMessage>
    {
        new(ChatRole.User, "I'm learning C#. Can you explain what is a record type?"),
    };

    Console.WriteLine("User: I'm learning C#. Can you explain what is a record type?");
    var response2 = await chatClient.GetResponseAsync(conversation);
    Console.WriteLine($"Assistant: {response2.Text}");
    Console.WriteLine();

    // Add assistant's response to conversation
    conversation.Add(response2.Messages[0]);

    // Follow-up question
    conversation.Add(new ChatMessage(ChatRole.User, "Can you show me a simple example?"));
    Console.WriteLine("User: Can you show me a simple example?");

    var response3 = await chatClient.GetResponseAsync(conversation);
    Console.WriteLine($"Assistant: {response3.Text}");
    Console.WriteLine();

    // Example 3: Streaming response
    Console.WriteLine("Example 3: Streaming Response");
    Console.WriteLine("------------------------------");

    var streamMessages = new List<ChatMessage>
    {
        new(ChatRole.User, "Count from 1 to 5, with one number per line.")
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

    Console.WriteLine("Examples completed successfully! ✓");
}
catch (Exception ex)
{
    Console.WriteLine($"ERROR: {ex.Message}");
    Console.WriteLine();
    Console.WriteLine("Stack Trace:");
    Console.WriteLine(ex.StackTrace);
}
