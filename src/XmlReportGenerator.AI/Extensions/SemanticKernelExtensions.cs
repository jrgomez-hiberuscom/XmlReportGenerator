using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using XmlReportGenerator.AI.Services;

namespace XmlReportGenerator.AI.Extensions;

/// <summary>
/// Extension methods for registering Semantic Kernel with the DI container.
/// </summary>
public static class SemanticKernelExtensions
{
    /// <summary>
    /// Registers the Semantic Kernel with the configured LLM provider from <paramref name="configuration"/>.
    /// When the endpoint is a LiteLLM-compatible API, a <see cref="LiteLlmClient"/> is also registered
    /// to bypass Semantic Kernel's response parser (which can fail with reasoning models).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddSemanticKernel(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var defaultProvider = configuration["AI:DefaultProvider"] ?? "GITHUBMODELS";

        var builder = Kernel.CreateBuilder();

        switch (defaultProvider.ToUpperInvariant())
        {
            case "GITHUBMODELS":
            default:
                var apiKey = configuration["AI:GitHubModels:ApiKey"]
                    ?? throw new InvalidOperationException("AI:GitHubModels:ApiKey is not configured.");
                var endpoint = configuration["AI:GitHubModels:Endpoint"]
                    ?? "https://models.inference.ai.azure.com";
                var model = configuration["AI:GitHubModels:Model"] ?? "gpt-4o";

                builder.AddOpenAIChatCompletion(
                    modelId: model,
                    apiKey: apiKey,
                    endpoint: new Uri(endpoint)
                );

                // Register LiteLlmClient for endpoints that are not the default GitHub Models API.
                // These are typically LiteLLM proxies or reasoning models that return extra fields
                // (e.g. reasoning_content) that confuse Semantic Kernel's response parser.
                if (!endpoint.Contains("models.inference.ai.azure.com", StringComparison.OrdinalIgnoreCase))
                {
                    services.AddSingleton(sp =>
                    {
                        var http = new HttpClient { BaseAddress = new Uri(endpoint) };
                        http.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                        var logger = sp.GetRequiredService<ILogger<LiteLlmClient>>();
                        return new LiteLlmClient(http, model, logger);
                    });
                }

                break;
        }

        var kernel = builder.Build();
        services.AddSingleton(kernel);

        return services;
    }
}
