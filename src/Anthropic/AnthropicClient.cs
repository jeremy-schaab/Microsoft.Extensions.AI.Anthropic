using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Anthropic.Core;
using Anthropic.Exceptions;
using Anthropic.Services;

namespace Anthropic;

public class AnthropicClient : IAnthropicClient
{
    private static readonly ThreadLocal<Random> _threadLocalRandom = new(() => new Random());

    private static Random Random => _threadLocalRandom.Value!;

    private readonly ClientOptions _options;

    public HttpClient HttpClient
    {
        get => _options.HttpClient;
        init => _options.HttpClient = value;
    }

    public Uri BaseUrl
    {
        get => _options.BaseUrl;
        init => _options.BaseUrl = value;
    }

    public bool ResponseValidation
    {
        get => _options.ResponseValidation;
        init => _options.ResponseValidation = value;
    }

    public int? MaxRetries
    {
        get => _options.MaxRetries;
        init => _options.MaxRetries = value;
    }

    public TimeSpan? Timeout
    {
        get => _options.Timeout;
        init => _options.Timeout = value;
    }

    public virtual string? APIKey
    {
        get => _options.APIKey;
        init => _options.APIKey = value;
    }

    public virtual string? AuthToken
    {
        get => _options.AuthToken;
        init => _options.AuthToken = value;
    }

    public virtual IAnthropicClient WithOptions(Func<ClientOptions, ClientOptions> modifier)
    {
        return new AnthropicClient(modifier(_options));
    }

    private readonly Lazy<IMessageService> _messages;
    public virtual IMessageService Messages => _messages.Value;

    private readonly Lazy<IModelService> _models;
    public virtual IModelService Models => _models.Value;

    private readonly Lazy<IBetaService> _beta;
    public virtual IBetaService Beta => _beta.Value;

    public async Task<HttpResponse> Execute<T>(
        HttpRequest<T> request,
        CancellationToken cancellationToken = default
    )
        where T : ParamsBase
    {
        var maxRetries = MaxRetries ?? ClientOptions.DefaultMaxRetries;
        if (maxRetries <= 0)
        {
            return await ExecuteOnce(request, cancellationToken).ConfigureAwait(false);
        }

        var retries = 0;
        while (true)
        {
            HttpResponse? response = null;
            try
            {
                response = await ExecuteOnce(request, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                if (++retries > maxRetries || !ShouldRetry(e))
                {
                    throw;
                }
            }

            if (response != null && (++retries > maxRetries || !ShouldRetry(response)))
            {
                if (response.Message.IsSuccessStatusCode)
                {
                    return response;
                }

                try
                {
                    throw AnthropicExceptionFactory.CreateApiException(
                        response.Message.StatusCode,
                        await response.ReadAsString(cancellationToken).ConfigureAwait(false)
                    );
                }
                catch (HttpRequestException e)
                {
                    throw new AnthropicIOException("I/O Exception", e);
                }
                finally
                {
                    response.Dispose();
                }
            }

            var backoff = ComputeRetryBackoff(retries, response);
            response?.Dispose();
            await Task.Delay(backoff, cancellationToken).ConfigureAwait(false);
        }
    }

    protected virtual ValueTask BeforeSend<T>(HttpRequest<T> request, HttpRequestMessage requestMessage, CancellationToken cancellationToken)
        where T : ParamsBase
    {
        return ValueTask.CompletedTask;
    }

    protected virtual ValueTask AfterSend<T>(HttpRequest<T> request, HttpResponseMessage httpResponseMessage, CancellationToken cancellationToken)
        where T : ParamsBase
    {
        return ValueTask.CompletedTask;
    }

    private async Task<HttpResponse> ExecuteOnce<T>(
        HttpRequest<T> request,
        CancellationToken cancellationToken = default
    )
        where T : ParamsBase
    {
        using HttpRequestMessage requestMessage = new(
            request.Method,
            request.Params.Url(_options)
        )
        {
            Content = request.Params.BodyContent(),
        };
        request.Params.AddHeadersToRequest(requestMessage, _options);
        using CancellationTokenSource timeoutCts = new(
            Timeout ?? ClientOptions.DefaultTimeout
        );
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(
            timeoutCts.Token,
            cancellationToken
        );
        HttpResponseMessage responseMessage;
        try
        {
            await BeforeSend(request, requestMessage, cts.Token).ConfigureAwait(false);
            responseMessage = await HttpClient.SendAsync(
                    requestMessage,
                    HttpCompletionOption.ResponseHeadersRead,
                    cts.Token
                )
                .ConfigureAwait(false);
            await AfterSend(request, responseMessage, cts.Token).ConfigureAwait(false);
        }
        catch (HttpRequestException e)
        {
            throw new AnthropicIOException("I/O exception", e);
        }
        return new HttpResponse { Message = responseMessage, CancellationToken = cts.Token };
    }

    private static TimeSpan ComputeRetryBackoff(int retries, HttpResponse? response)
    {
        TimeSpan? apiBackoff = ParseRetryAfterMsHeader(response) ?? ParseRetryAfterHeader(response);
        if (apiBackoff != null && apiBackoff < TimeSpan.FromMinutes(1))
        {
            // If the API asks us to wait a certain amount of time (and it's a reasonable amount), then just
            // do what it says.
            return (TimeSpan)apiBackoff;
        }

        // Apply exponential backoff, but not more than the max.
        var backoffSeconds = Math.Min(0.5 * Math.Pow(2.0, retries - 1), 8.0);
        var jitter = 1.0 - 0.25 * Random.NextDouble();
        return TimeSpan.FromSeconds(backoffSeconds * jitter);
    }

    private static TimeSpan? ParseRetryAfterMsHeader(HttpResponse? response)
    {
        IEnumerable<string>? headerValues = null;
        response?.Message.Headers.TryGetValues("Retry-After-Ms", out headerValues);
        var headerValue = headerValues == null ? null : Enumerable.FirstOrDefault(headerValues);
        if (headerValue == null)
        {
            return null;
        }

        if (float.TryParse(headerValue
#if NET5_0_OR_GREATER
            .AsSpan()
#endif
        , out var retryAfterMs))
        {
            return TimeSpan.FromMilliseconds(retryAfterMs);
        }

        return null;
    }

    private static TimeSpan? ParseRetryAfterHeader(HttpResponse? response)
    {
        IEnumerable<string>? headerValues = null;
        response?.Message.Headers.TryGetValues("Retry-After", out headerValues);
        var headerValue = headerValues == null ? null : Enumerable.FirstOrDefault(headerValues);
        if (headerValue == null)
        {
            return null;
        }

        if (float.TryParse(headerValue
#if NET5_0_OR_GREATER
            .AsSpan()
#endif
        , out var retryAfterSeconds))
        {
            return TimeSpan.FromSeconds(retryAfterSeconds);
        }
        else if (DateTimeOffset.TryParse(headerValue
#if NET5_0_OR_GREATER
            .AsSpan()
#endif
        , out var retryAfterDate))
        {
            return retryAfterDate - DateTimeOffset.Now;
        }

        return null;
    }

    private static bool ShouldRetry(HttpResponse response)
    {
        if (
            response.Message.Headers.TryGetValues("X-Should-Retry", out var headerValues)
            && bool.TryParse(Enumerable.FirstOrDefault(headerValues), out var shouldRetry)
        )
        {
            // If the server explicitly says whether to retry, then we obey.
            return shouldRetry;
        }

        return response.Message.StatusCode switch
        {
            // Retry on request timeouts
            HttpStatusCode.RequestTimeout
            or
            // Retry on lock timeouts
            HttpStatusCode.Conflict
            or
#if !NETSTANDARD2_0_OR_GREATER
            // Retry on rate limits
            HttpStatusCode.TooManyRequests            
            or
#endif
            // Retry internal errors
            >= HttpStatusCode.InternalServerError => true,
            _ => false,
        };
    }

    private static bool ShouldRetry(Exception e)
    {
        return e is IOException || e is AnthropicIOException;
    }

    public AnthropicClient()
    {
        _options = new ClientOptions();

        _messages = new Lazy<IMessageService>(() => new MessageService(this));
        _models = new Lazy<IModelService>(() => new ModelService(this));
        _beta = new Lazy<IBetaService>(() => new BetaService(this));
    }

    public AnthropicClient(ClientOptions options)
        : this()
    {
        _options = options;
    }
}
