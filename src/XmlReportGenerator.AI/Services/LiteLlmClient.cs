using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace XmlReportGenerator.AI.Services;

/// <summary>
/// Lightweight HTTP client for LiteLLM / OpenAI-compatible APIs.
/// Retries transient errors (5xx, timeout) with exponential back-off.
/// </summary>
public class LiteLlmClient
{
    private readonly HttpClient _http;
    private readonly string _model;
    private readonly ILogger<LiteLlmClient> _logger;

    /// <summary>HTTP status codes considered transient and eligible for retry.</summary>
    private static readonly HashSet<HttpStatusCode> RetryableStatusCodes =
    [
        HttpStatusCode.GatewayTimeout,          // 504
        HttpStatusCode.BadGateway,              // 502
        HttpStatusCode.ServiceUnavailable,      // 503
        HttpStatusCode.InternalServerError,     // 500
        HttpStatusCode.RequestTimeout,          // 408
        HttpStatusCode.TooManyRequests,         // 429
    ];

    /// <summary>Maximum number of attempts (1 initial + N retries).</summary>
    private readonly int _maxAttempts;

    /// <summary>Base delay in milliseconds for exponential back-off.</summary>
    private readonly int _baseDelayMs;

    public LiteLlmClient(HttpClient http, string model, ILogger<LiteLlmClient> logger,
        int? maxAttempts = null, int? baseDelayMs = null)
    {
        _http = http;
        _model = model;
        _logger = logger;
        _maxAttempts = maxAttempts ?? 4;
        _baseDelayMs = baseDelayMs ?? 2000;
    }

    /// <summary>
    /// Sends a chat completion request and returns the assistant message content.
    /// Retries up to <see cref="MaxAttempts"/> times on transient failures.
    /// </summary>
    public async Task<string> ChatAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default)
    {
        var requestBody = new
        {
            model = _model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user",   content = userMessage  }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);

        _logger.LogDebug("LiteLLM request to model {Model} ({Chars} chars)", _model, userMessage.Length);

        for (int attempt = 1; attempt <= _maxAttempts; attempt++)
        {
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            try
            {
                response = await _http.PostAsync("/chat/completions", content, cancellationToken);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                // HTTP timeout (not user cancellation)
                if (attempt == _maxAttempts)
                    throw new HttpRequestException($"LiteLLM request timed out after {attempt} attempt(s).", ex);

                await DelayBeforeRetryAsync(attempt, "timeout", cancellationToken);
                continue;
            }

            if (response.IsSuccessStatusCode)
            {
                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

                var messageContent = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? string.Empty;

                _logger.LogDebug("LiteLLM response ({Chars} chars): {Preview}",
                    messageContent.Length,
                    messageContent.Length > 0 ? messageContent[..Math.Min(300, messageContent.Length)] : "(empty)");

                return messageContent;
            }

            if (RetryableStatusCodes.Contains(response.StatusCode) && attempt < _maxAttempts)
            {
                await DelayBeforeRetryAsync(attempt, $"HTTP {(int)response.StatusCode} {response.StatusCode}", cancellationToken);
                continue;
            }

            // Non-retryable error or last attempt
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"LiteLLM returned HTTP {(int)response.StatusCode} after {attempt} attempt(s): {errorBody}",
                inner: null,
                statusCode: response.StatusCode);
        }

        // Should never reach here
        throw new InvalidOperationException("LiteLLM retry loop exited unexpectedly.");
    }

    private async Task DelayBeforeRetryAsync(int attempt, string reason, CancellationToken cancellationToken)
    {
        var delayMs = _baseDelayMs * (int)Math.Pow(2, attempt - 1); // e.g. 2s, 4s, 8s
        _logger.LogWarning(
            "LiteLLM transient error ({Reason}). Retrying in {Delay}ms (attempt {Attempt}/{Max})…",
            reason, delayMs, attempt, _maxAttempts);
        await Task.Delay(delayMs, cancellationToken);
    }
}
