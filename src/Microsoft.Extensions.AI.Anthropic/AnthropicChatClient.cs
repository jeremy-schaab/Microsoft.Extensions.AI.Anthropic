using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Anthropic;
using Anthropic.Core;
using Anthropic.Foundry;
using Anthropic.Services;
using Microsoft.Extensions.AI;

namespace Microsoft.Extensions.AI.Anthropic;

/// <summary>
/// An <see cref="IChatClient"/> implementation for Anthropic's Claude models.
/// Supports both standard Anthropic API (<see cref="AnthropicClient"/>) and
/// Azure Anthropic Foundry (<see cref="AnthropicFoundryClient"/>).
/// </summary>
/// <remarks>
/// This implementation follows the established pattern from Microsoft.Extensions.AI.OpenAI,
/// providing seamless integration with the Microsoft.Extensions.AI abstractions framework.
///
/// <para>
/// <strong>Authentication</strong>:
/// For Azure Foundry, use <see cref="AnthropicFoundryClient"/> with appropriate credentials
/// (API Key, Bearer Token, or Azure Identity). For standard API, use <see cref="AnthropicClient"/>
/// with your Anthropic API key.
/// </para>
///
/// <para>
/// <strong>System Messages</strong>:
/// System messages are automatically extracted from the message array and sent via
/// Anthropic's separate <c>system</c> parameter, as required by the Anthropic API.
/// </para>
///
/// <para>
/// <strong>Tool Calling</strong>:
/// Full support for Claude's tool use capabilities. Tools are automatically converted
/// between Microsoft.Extensions.AI and Anthropic formats.
/// </para>
/// </remarks>
public sealed class AnthropicChatClient : IChatClient
{
    private readonly IAnthropicClient _anthropicClient;
    private readonly IMessageService _messageService;
    private readonly string? _modelId;


    /// <summary>
    /// Initializes a new instance of <see cref="AnthropicChatClient"/> with an API key and resource name.
    /// </summary>
    /// <param name="apiKey">The API key for authentication.</param>
    /// <param name="resourceName">The resource name for the Anthropic service.</param>
    /// <param name="modelId"></param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="apiKey"/> is null.</exception>
    public AnthropicChatClient(string apiKey, string resourceName, string? modelId = null)
    {
        ArgumentNullException.ThrowIfNull(apiKey);
        var credentials = new AnthropicFoundryApiKeyCredentials(
            apiKey: apiKey,
            resourceName: resourceName);

        var foundryClient = new AnthropicFoundryClient(credentials);
        _anthropicClient = foundryClient;
        _modelId = modelId;
        _messageService = foundryClient.Messages;
        // Detect if this is Azure Foundry by checking the type
        var isAzureFoundry = true;

        // Attempt to get endpoint information
        var endpoint = TryGetEndpoint(foundryClient);

        // Create metadata
        Metadata = new ChatClientMetadata(
            providerName: isAzureFoundry ? "anthropic-foundry" : "anthropic",
            providerUri: endpoint,
            defaultModelId: _modelId);
    }
    /// <summary>
    /// Initializes a new instance of <see cref="AnthropicChatClient"/> that wraps an Anthropic client.
    /// </summary>
    /// <param name="anthropicClient">
    /// The Anthropic client instance. Can be either <see cref="AnthropicClient"/> (standard API)
    /// or <see cref="AnthropicFoundryClient"/> (Azure-hosted).
    /// </param>
    /// <param name="modelId">
    /// Optional model ID to use for requests. If not specified here, must be provided in
    /// <see cref="ChatOptions.ModelId"/> for each request. Examples: "claude-sonnet-4-5",
    /// "claude-opus-4", "claude-haiku-4".
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="anthropicClient"/> is null.</exception>
    public AnthropicChatClient(IAnthropicClient anthropicClient, string? modelId = null)
    {
        ArgumentNullException.ThrowIfNull(anthropicClient);

        _anthropicClient = anthropicClient;
        _messageService = anthropicClient.Messages;
        _modelId = modelId;

        // Detect if this is Azure Foundry by checking the type
        var isAzureFoundry = anthropicClient.GetType().Name.Contains("Foundry", StringComparison.OrdinalIgnoreCase);

        // Attempt to get endpoint information
        var endpoint = TryGetEndpoint(anthropicClient);

        // Create metadata
        Metadata = new ChatClientMetadata(
            providerName: isAzureFoundry ? "anthropic-foundry" : "anthropic",
            providerUri: endpoint,
            defaultModelId: _modelId);
    }

    /// <summary>
    /// Initializes a new instance of <see cref="AnthropicChatClient"/> that wraps a message service.
    /// </summary>
    /// <param name="messageService">The Anthropic message service to use for API calls.</param>
    /// <param name="modelId">
    /// Optional model ID to use for requests. If not specified here, must be provided in
    /// <see cref="ChatOptions.ModelId"/> for each request.
    /// </param>
    /// <param name="endpoint">Optional endpoint URI for metadata.</param>
    /// <param name="isAzureFoundry">Whether this client connects to Azure Foundry (true) or standard API (false).</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="messageService"/> is null.</exception>
    public AnthropicChatClient(
        IMessageService messageService,
        string? modelId = null,
        Uri? endpoint = null,
        bool isAzureFoundry = false)
    {
        ArgumentNullException.ThrowIfNull(messageService);

        _messageService = messageService;
        _anthropicClient = null!; // When using message service directly, client may not be available
        _modelId = modelId;
        var endpoint1 = endpoint;
        var isAzureFoundry1 = isAzureFoundry;

        Metadata = new ChatClientMetadata(
            providerName: isAzureFoundry1 ? "anthropic-foundry" : "anthropic",
            providerUri: endpoint1,
            defaultModelId: _modelId);
    }

    /// <summary>
    /// Meta Data Property
    /// </summary>
    public ChatClientMetadata Metadata { get; }

    /// <inheritdoc/>
    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(chatMessages);

        // Convert messages and options to Anthropic format
        var (messages, systemPrompt) = AnthropicMessageConverter.ToAnthropicMessages(chatMessages);
        var createParams = AnthropicOptionsConverter.ToMessageCreateParams(
            messages,
            systemPrompt,
            options,
            _modelId);

        // Validate that we have a model ID
        if (string.IsNullOrWhiteSpace(createParams.Model))
        {
            throw new InvalidOperationException(
                "Model ID must be specified either in the constructor or in ChatOptions.ModelId");
        }

        // Call Anthropic API
        var response = await _messageService.Create(createParams, cancellationToken).ConfigureAwait(false);

        // Convert response back to Microsoft.Extensions.AI format
        return AnthropicMessageConverter.FromAnthropicMessage(response, Metadata);
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(chatMessages);

        // Convert messages and options to Anthropic format
        var (messages, systemPrompt) = AnthropicMessageConverter.ToAnthropicMessages(chatMessages);
        var createParams = AnthropicOptionsConverter.ToMessageCreateParams(
            messages,
            systemPrompt,
            options,
            _modelId);

        // Validate that we have a model ID
        if (string.IsNullOrWhiteSpace(createParams.Model))
        {
            throw new InvalidOperationException(
                "Model ID must be specified either in the constructor or in ChatOptions.ModelId");
        }

        // Call Anthropic streaming API and convert events
        var streamingEvents = _messageService.CreateStreaming(createParams, cancellationToken);

        await foreach (var update in AnthropicStreamingConverter.ConvertStreamAsync(
            streamingEvents,
            Metadata,
            cancellationToken).ConfigureAwait(false))
        {
            yield return update;
        }
    }

    /// <inheritdoc/>
    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        ArgumentNullException.ThrowIfNull(serviceType);

        // Return self if IChatClient or AnthropicChatClient is requested (no key)
        if (serviceKey == null && serviceType.IsInstanceOfType(this))
        {
            return this;
        }

        // Return metadata if requested
        if (serviceKey == null && serviceType == typeof(ChatClientMetadata))
        {
            return Metadata;
        }

        // Return the underlying Anthropic client if requested
        if (_anthropicClient is not null && serviceKey == null && serviceType.IsInstanceOfType(_anthropicClient))
        {
            return _anthropicClient;
        }

        // Return the message service if requested
        if (serviceKey == null && serviceType.IsInstanceOfType(_messageService))
        {
            return _messageService;
        }

        return null;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // Anthropic SDK clients implement IDisposable
        if (_anthropicClient is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    /// <summary>
    /// Attempts to extract the endpoint URI from the Anthropic client using reflection.
    /// </summary>
    private static Uri? TryGetEndpoint(IAnthropicClient client)
    {
        try
        {
            // Try to get the base URL from the client
            var baseUrlProperty = client.GetType().GetProperty("BaseUrl");
            if (baseUrlProperty?.GetValue(client) is string baseUrl)
            {
                return new Uri(baseUrl);
            }
        }
        catch
        {
            // Ignore reflection errors - endpoint is optional metadata
        }

        return null;
    }
}
