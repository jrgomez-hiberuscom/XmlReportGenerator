using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XmlReportGenerator.AI.Services;

namespace XmlReportGenerator.AI.Extensions;

/// <summary>
/// Extension methods for registering AI services with the DI container.
/// </summary>
public static class AiExtensions
{
    /// <summary>
    /// Registers a <see cref="LiteLlmClient"/> configured from <c>AI:GitHubModels</c> settings.
    /// </summary>
    public static IServiceCollection AddAiServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var apiKey = configuration["AI:GitHubModels:ApiKey"]
            ?? throw new InvalidOperationException("AI:GitHubModels:ApiKey is not configured.");
        var endpoint = configuration["AI:GitHubModels:Endpoint"]
            ?? "https://models.inference.ai.azure.com";
        var model = configuration["AI:GitHubModels:Model"]
            ?? throw new InvalidOperationException("AI:GitHubModels:Model is not configured.");

        services.AddHttpClient<LiteLlmClient>(http =>
        {
            http.BaseAddress = new Uri(endpoint);
            http.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        });

        // Bind model name via factory so LiteLlmClient receives it
        var maxAttempts = configuration.GetValue<int?>("AI:GitHubModels:Retry:MaxAttempts");
        var baseDelayMs = configuration.GetValue<int?>("AI:GitHubModels:Retry:BaseDelayMs");

        services.AddSingleton(sp =>
        {
            var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(LiteLlmClient));
            var logger = sp.GetRequiredService<ILogger<LiteLlmClient>>();
            return new LiteLlmClient(http, model, logger, maxAttempts, baseDelayMs);
        });

        return services;
    }
}
