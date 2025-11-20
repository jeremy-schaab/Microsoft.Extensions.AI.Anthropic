using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Anthropic;
using Microsoft.Extensions.DependencyInjection;
using Anthropic.Foundry;
using Azure.Identity;
using System.Text;

namespace StreamingChatExample;

/// <summary>
/// Demonstrates real-time streaming chat with Microsoft.Extensions.AI.Anthropic
/// Supports both Azure Foundry and standard Anthropic API
/// </summary>
class Program
{
    private static readonly Lock _consoleLock = new();
    private static CancellationTokenSource? _cts;

    static async Task Main(string[] args)
    {
        // Set up Ctrl+C cancellation
        _cts = new CancellationTokenSource();
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            _cts?.Cancel();
            WriteColoredLine("\n\nCancellation requested. Shutting down...", ConsoleColor.Yellow);
        };

        try
        {
            WriteColoredLine("=== Microsoft.Extensions.AI.Anthropic Streaming Chat ===\n", ConsoleColor.Cyan);

            // Initialize the appropriate client based on environment
            IChatClient chatClient = CreateChatClient();

            WriteColoredLine($"Using: {(IsAzureFoundry() ? "Azure Anthropic Foundry" : "Standard Anthropic API")}", ConsoleColor.Green);
            WriteColoredLine("Model: claude-3-5-sonnet-20241022\n", ConsoleColor.Green);
            WriteColoredLine("Type 'exit' or 'quit' to end the conversation", ConsoleColor.DarkGray);
            WriteColoredLine("Press Ctrl+C to cancel current response\n", ConsoleColor.DarkGray);

            // Maintain conversation history
            var conversationHistory = new List<ChatMessage>();

            while (!_cts.Token.IsCancellationRequested)
            {
                // Get user input
                WriteColored("\nYou: ", ConsoleColor.Yellow);
                var userInput = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(userInput))
                    continue;

                if (userInput.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
                    userInput.Equals("quit", StringComparison.OrdinalIgnoreCase))
                {
                    WriteColoredLine("\nGoodbye!", ConsoleColor.Cyan);
                    break;
                }

                // Add user message to history
                conversationHistory.Add(new ChatMessage(ChatRole.User, userInput));

                // Display assistant response header
                WriteColored("\nAssistant: ", ConsoleColor.Cyan);

                try
                {
                    // Stream the response
                    var response = await StreamResponseAsync(chatClient, conversationHistory, _cts.Token);

                    // Add assistant response to history
                    conversationHistory.Add(new ChatMessage(ChatRole.Assistant, response.Content));

                    // Display usage statistics
                    DisplayUsageStatistics(response);
                }
                catch (OperationCanceledException)
                {
                    WriteColoredLine("\n\n[Response cancelled by user]", ConsoleColor.Yellow);
                    // Remove the last user message since we didn't complete the response
                    if (conversationHistory.Count > 0)
                        conversationHistory.RemoveAt(conversationHistory.Count - 1);

                    // Reset cancellation token
                    _cts = new CancellationTokenSource();
                    Console.CancelKeyPress += (sender, e) =>
                    {
                        e.Cancel = true;
                        _cts?.Cancel();
                    };
                }
                catch (Exception ex)
                {
                    WriteColoredLine($"\n\nError: {ex.Message}", ConsoleColor.Red);
                    if (ex.InnerException != null)
                        WriteColoredLine($"Inner: {ex.InnerException.Message}", ConsoleColor.Red);
                }
            }
        }
        catch (Exception ex)
        {
            WriteColoredLine($"\nFatal error: {ex.Message}", ConsoleColor.Red);
            if (ex.InnerException != null)
                WriteColoredLine($"Inner: {ex.InnerException.Message}", ConsoleColor.Red);
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// Streams the chat response and displays tokens in real-time
    /// </summary>
    private static async Task<StreamingResponse> StreamResponseAsync(
        IChatClient chatClient,
        List<ChatMessage> conversationHistory,
        CancellationToken cancellationToken)
    {
        var contentBuilder = new StringBuilder();
        var updates = new List<ChatResponseUpdate>();

        var options = new ChatOptions
        {
            Temperature = 0.7f,
            MaxOutputTokens = 2048
        };

        await foreach (var update in chatClient.GetStreamingResponseAsync(
            conversationHistory,
            options,
            cancellationToken))
        {
            updates.Add(update);

            // Stream text content to console in real-time
            if (update.Text != null)
            {
                lock (_consoleLock)
                {
                    Console.Write(update.Text);
                    contentBuilder.Append(update.Text);
                }
            }

            // Handle tool calls (if any)
            if (update.Contents != null)
            {
                foreach (var content in update.Contents)
                {
                    if (content is FunctionCallContent toolCall)
                    {
                        lock (_consoleLock)
                        {
                            WriteColoredLine($"\n[Tool Call: {toolCall.Name}]", ConsoleColor.Magenta);
                        }
                    }
                }
            }
        }

        Console.WriteLine(); // New line after streaming completes

        // Aggregate final response from updates
        var lastUpdate = updates.LastOrDefault();

        // Extract usage from UsageContent if available
        UsageDetails? usage = null;
        if (lastUpdate?.Contents != null)
        {
            var usageContent = lastUpdate.Contents.OfType<UsageContent>().FirstOrDefault();
            usage = usageContent?.Details;
        }

        return new StreamingResponse
        {
            Content = contentBuilder.ToString(),
            Usage = usage,
            FinishReason = lastUpdate?.FinishReason,
            ModelId = lastUpdate?.ModelId
        };
    }

    /// <summary>
    /// Displays usage statistics after response completion
    /// </summary>
    private static void DisplayUsageStatistics(StreamingResponse response)
    {
        if (response.Usage != null)
        {
            WriteColored("\n[", ConsoleColor.DarkGray);
            WriteColored($"Tokens: {response.Usage.InputTokenCount} in", ConsoleColor.DarkGray);
            WriteColored(" / ", ConsoleColor.DarkGray);
            WriteColored($"{response.Usage.OutputTokenCount} out", ConsoleColor.DarkGray);
            WriteColored(" / ", ConsoleColor.DarkGray);
            WriteColored($"{response.Usage.TotalTokenCount} total", ConsoleColor.DarkGray);

            if (response.FinishReason != null)
            {
                WriteColored($" | Stop: {response.FinishReason}", ConsoleColor.DarkGray);
            }

            WriteColoredLine("]", ConsoleColor.DarkGray);
        }
    }

    /// <summary>
    /// Creates the appropriate chat client based on environment variables
    /// </summary>
    private static IChatClient CreateChatClient()
    {
        if (IsAzureFoundry())
        {
            return CreateAzureFoundryClient();
        }
        else
        {
            return CreateStandardAnthropicClient();
        }
    }

    /// <summary>
    /// Checks if Azure Foundry configuration is present
    /// </summary>
    private static bool IsAzureFoundry()
    {
        return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ANTHROPIC_FOUNDRY_RESOURCE"));
    }

    /// <summary>
    /// Creates Azure Foundry client with appropriate authentication
    /// </summary>
    private static IChatClient CreateAzureFoundryClient()
    {
        var resource = Environment.GetEnvironmentVariable("ANTHROPIC_FOUNDRY_RESOURCE")
            ?? throw new InvalidOperationException("ANTHROPIC_FOUNDRY_RESOURCE environment variable not set");

        var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_FOUNDRY_API_KEY");

        var services = new ServiceCollection();

        if (!string.IsNullOrEmpty(apiKey))
        {
            // Use API key authentication
            services.AddAnthropicFoundryChatClient(resource, apiKey, "claude-3-5-sonnet-20241022");
        }
        else
        {
            // Use Azure Identity from environment
            services.AddAnthropicFoundryChatClientFromEnvironment(resource, "claude-3-5-sonnet-20241022");
        }

        var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<IChatClient>();
    }

    /// <summary>
    /// Creates standard Anthropic API client
    /// </summary>
    private static IChatClient CreateStandardAnthropicClient()
    {
        var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")
            ?? throw new InvalidOperationException(
                "Neither ANTHROPIC_FOUNDRY_RESOURCE nor ANTHROPIC_API_KEY environment variable is set.\n" +
                "Set one of:\n" +
                "  - ANTHROPIC_FOUNDRY_RESOURCE=<resource-name> (and optionally ANTHROPIC_FOUNDRY_API_KEY)\n" +
                "  - ANTHROPIC_API_KEY=<your-api-key>");

        var services = new ServiceCollection();
        services.AddAnthropicChatClient(apiKey, "claude-3-5-sonnet-20241022");

        var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<IChatClient>();
    }

    #region Console Helpers

    private static void WriteColored(string text, ConsoleColor color)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ForegroundColor = originalColor;
    }

    private static void WriteColoredLine(string text, ConsoleColor color)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ForegroundColor = originalColor;
    }

    #endregion

    /// <summary>
    /// Response container for streaming results
    /// </summary>
    private class StreamingResponse
    {
        public string Content { get; set; } = string.Empty;
        public UsageDetails? Usage { get; set; }
        public ChatFinishReason? FinishReason { get; set; }
        public string? ModelId { get; set; }
    }
}
