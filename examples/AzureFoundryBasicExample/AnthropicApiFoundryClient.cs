using Anthropic;
using Anthropic.Core;
using Anthropic.Foundry;

namespace AzureFoundryBasicExample;

public class AnthropicApiFoundryClient : AnthropicClient
{
    private readonly IAnthropicFoundryCredentials _azureCredentials;

    /// <summary>
    /// Creates a new instance of the <see cref="AnthropicFoundryClient"/>.
    /// </summary>
    /// <param name="azureCredentials">The credential provider. Use the <see cref="IAnthropicFoundryCredentials.FromEnv"/> to generate a set of credentials in supported environments or use another implementation for static assignment of credentials.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public AnthropicApiFoundryClient(IAnthropicFoundryCredentials azureCredentials)
    {
        _azureCredentials = azureCredentials ?? throw new ArgumentNullException(nameof(azureCredentials));
        var url = $"https://{azureCredentials.ResourceName}.services.ai.azure.com/anthropic/";
        BaseUrl = new Uri(url, UriKind.Absolute);
        Console.WriteLine("Base URL " + BaseUrl);
    }

    [Obsolete("The {nameof(APIKey)} property is not supported in this configuration.", true)]
#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member
    public override string? APIKey
#pragma warning restore CS0809 // Obsolete member overrides non-obsolete member
    {
        get => throw new NotSupportedException($"The {nameof(APIKey)} property is not supported in this configuration.");
        init => throw new NotSupportedException($"The {nameof(APIKey)} property is not supported in this configuration.");
    }

    protected override ValueTask BeforeSend<T>(HttpRequest<T> request, HttpRequestMessage requestMessage, CancellationToken cancellationToken)
    {
        _azureCredentials.Apply(requestMessage);

        return ValueTask.CompletedTask;
    }
}