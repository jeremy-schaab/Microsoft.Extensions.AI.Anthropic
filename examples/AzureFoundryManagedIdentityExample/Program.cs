using Anthropic.Foundry;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Anthropic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzureFoundryManagedIdentityExample;

/// <summary>
/// Production-ready example demonstrating Azure Managed Identity authentication
/// with Anthropic Foundry for enterprise deployments.
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        // Configure logging
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.SetMinimumLevel(LogLevel.Information);

        // Load configuration from appsettings.json and environment variables
        builder.Configuration
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .AddUserSecrets<Program>(optional: true);

        // Get Anthropic Foundry settings
        var foundrySettings = builder.Configuration
            .GetSection("AnthropicFoundry")
            .Get<AnthropicFoundrySettings>() ?? throw new InvalidOperationException("AnthropicFoundry configuration is missing");

        // Validate configuration
        if (string.IsNullOrWhiteSpace(foundrySettings.ResourceName))
        {
            throw new InvalidOperationException("AnthropicFoundry:ResourceName is required");
        }

        // Configure DefaultAzureCredential with detailed options
        var credentialOptions = new DefaultAzureCredentialOptions
        {
            // Exclude interactive credentials for production scenarios
            ExcludeInteractiveBrowserCredential = !builder.Environment.IsDevelopment(),
            ExcludeVisualStudioCredential = !builder.Environment.IsDevelopment(),
            ExcludeVisualStudioCodeCredential = !builder.Environment.IsDevelopment(),
            ExcludeAzurePowerShellCredential = !builder.Environment.IsDevelopment(),

            // Production credentials
            ExcludeManagedIdentityCredential = false,
            ExcludeEnvironmentCredential = false,
            ExcludeAzureCliCredential = false,

            // Optional: Specify managed identity client ID for user-assigned identity
            ManagedIdentityClientId = foundrySettings.ManagedIdentityClientId,

            // Tenant ID (optional, for single-tenant scenarios)
            TenantId = foundrySettings.TenantId,

            // Retry configuration
            Retry = {
                MaxRetries = 3,
                Delay = TimeSpan.FromSeconds(2),
                MaxDelay = TimeSpan.FromSeconds(16),
                Mode = Azure.Core.RetryMode.Exponential
            }
        };

        var credential = new DefaultAzureCredential(credentialOptions);

        // Register Anthropic Foundry chat client with Managed Identity using factory pattern
        builder.Services.AddAnthropicFoundryChatClient(
            clientFactory: sp =>
            {
                // Get an access token using the credential
                // Note: This is a simplified example. In production, you may want to handle token refresh.
                var tokenRequestContext = new Azure.Core.TokenRequestContext(
                    new[] { "https://cognitiveservices.azure.com/.default" }
                );
                var token = credential.GetToken(tokenRequestContext, default);

                // Create credentials with the access token
                var foundryCredentials = new AnthropicFoundryIdentityTokenCredentials(
                    token,
                    foundrySettings.ResourceName
                );

                return new AnthropicFoundryClient(foundryCredentials);
            },
            modelId: foundrySettings.ModelId ?? "claude-3-5-sonnet-20241022"
        );

        // Register the chat service
        builder.Services.AddSingleton<IChatService, ChatService>();

        var host = builder.Build();

        // Run the example
        var logger = host.Services.GetRequiredService<ILogger<Program>>();
        var chatService = host.Services.GetRequiredService<IChatService>();

        try
        {
            logger.LogInformation("Starting Azure Foundry Managed Identity Example");
            logger.LogInformation("Environment: {Environment}", builder.Environment.EnvironmentName);
            logger.LogInformation("Resource: {Resource}", foundrySettings.ResourceName);
            logger.LogInformation("Model: {Model}", foundrySettings.ModelId ?? "claude-3-5-sonnet-20241022");

            await chatService.RunExamplesAsync();

            logger.LogInformation("Example completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fatal error running example");
            throw;
        }
    }
}

/// <summary>
/// Configuration settings for Anthropic Foundry.
/// </summary>
public class AnthropicFoundrySettings
{
    /// <summary>
    /// Azure Anthropic Foundry resource name (required).
    /// Example: "my-anthropic-foundry"
    /// </summary>
    public string ResourceName { get; set; } = string.Empty;

    /// <summary>
    /// Claude model ID to use (optional, defaults to claude-3-5-sonnet-20241022).
    /// </summary>
    public string? ModelId { get; set; }

    /// <summary>
    /// Client ID for user-assigned managed identity (optional).
    /// If not specified, system-assigned identity is used.
    /// </summary>
    public string? ManagedIdentityClientId { get; set; }

    /// <summary>
    /// Azure AD tenant ID (optional, for single-tenant scenarios).
    /// </summary>
    public string? TenantId { get; set; }
}

/// <summary>
/// Service interface for chat operations.
/// </summary>
public interface IChatService
{
    Task RunExamplesAsync();
}

/// <summary>
/// Chat service demonstrating production patterns.
/// </summary>
public class ChatService : IChatService
{
    private readonly IChatClient _chatClient;
    private readonly ILogger<ChatService> _logger;

    public ChatService(IChatClient chatClient, ILogger<ChatService> logger)
    {
        _chatClient = chatClient;
        _logger = logger;
    }

    public async Task RunExamplesAsync()
    {
        await BasicChatExample();
        await StreamingChatExample();
        await MultiTurnConversationExample();
    }

    private async Task BasicChatExample()
    {
        _logger.LogInformation("=== Basic Chat Example ===");

        try
        {
            var messages = new List<ChatMessage>
            {
                new(ChatRole.User, "What is the capital of France? Respond in one sentence.")
            };

            var response = await _chatClient.GetResponseAsync(messages);

            _logger.LogInformation("Response: {Response}", response.Text);

            // Extract usage information
            var usageContent = response.Messages[0].Contents.OfType<UsageContent>().FirstOrDefault();
            if (usageContent is not null)
            {
                _logger.LogInformation("Usage - Input: {Input} tokens, Output: {Output} tokens",
                    usageContent.Details.InputTokenCount,
                    usageContent.Details.OutputTokenCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in basic chat example");
            throw;
        }
    }

    private async Task StreamingChatExample()
    {
        _logger.LogInformation("\n=== Streaming Chat Example ===");

        try
        {
            var messages = new List<ChatMessage>
            {
                new(ChatRole.User, "Write a haiku about clouds.")
            };

            Console.Write("Response: ");

            await foreach (var update in _chatClient.GetStreamingResponseAsync(messages))
            {
                if (!string.IsNullOrEmpty(update.Text))
                {
                    Console.Write(update.Text);
                }
            }

            Console.WriteLine("\n");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in streaming chat example");
            throw;
        }
    }

    private async Task MultiTurnConversationExample()
    {
        _logger.LogInformation("=== Multi-Turn Conversation Example ===");

        try
        {
            var messages = new List<ChatMessage>
            {
                new(ChatRole.User, "My name is Alice and I live in Seattle."),
            };

            var response1 = await _chatClient.GetResponseAsync(messages);
            _logger.LogInformation("Turn 1 Response: {Response}", response1.Text);
            messages.Add(response1.Messages[0]);

            messages.Add(new ChatMessage(ChatRole.User, "What city did I say I live in?"));

            var response2 = await _chatClient.GetResponseAsync(messages);
            _logger.LogInformation("Turn 2 Response: {Response}", response2.Text);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in multi-turn conversation example");
            throw;
        }
    }
}
