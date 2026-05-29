using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace XmlReportGenerator.AI.Services;

/// <summary>
/// Lightweight HTTP client for LiteLLM / OpenAI-compatible APIs.
/// Used instead of Semantic Kernel when the model returns extra fields
/// (e.g. reasoning_content) that may confuse the SK response parser.
/// </summary>
public class LiteLlmClient
{
    private readonly HttpClient _http;
    private readonly string _model;
    private readonly ILogger<LiteLlmClient> _logger;

    public LiteLlmClient(HttpClient http, string model, ILogger<LiteLlmClient> logger)
    {
        _http = http;
        _model = model;
        _logger = logger;
    }

    /// <summary>
    /// Sends a chat completion request and returns the assistant message content.
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
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogDebug("LiteLLM request to model {Model} ({Chars} chars)", _model, userMessage.Length);

        var response = await _http.PostAsync("/chat/completions", content, cancellationToken);
        response.EnsureSuccessStatusCode();

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
}
