using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

#pragma warning disable SKEXP0070 // Experimental SK connectors

namespace XmlReportGenerator.AI.Extensions;

/// <summary>
/// Extension methods for registering Semantic Kernel with the DI container.
/// </summary>
public static class SemanticKernelExtensions
{
    /// <summary>
    /// Registers a <see cref="Kernel"/> instance configured from <c>appsettings.json</c>.
    /// The provider is selected via the <c>AI:DefaultProvider</c> key
    /// (supported values: <c>OpenAI</c>, <c>AzureOpenAI</c>, <c>Google</c>, <c>Anthropic</c>).
    /// </summary>
    /// <param name="services">The service collection to add the kernel to.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The modified service collection.</returns>
    public static IServiceCollection AddSemanticKernel(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var provider = configuration["AI:DefaultProvider"] ?? "OpenAI";

        var builder = Kernel.CreateBuilder();

        switch (provider.ToUpperInvariant())
        {
            case "AZUREOPENAI":
            {
                var endpoint = configuration["AI:AzureOpenAI:Endpoint"]
                    ?? throw new InvalidOperationException("AI:AzureOpenAI:Endpoint is not configured.");
                var apiKey = configuration["AI:AzureOpenAI:ApiKey"]
                    ?? throw new InvalidOperationException("AI:AzureOpenAI:ApiKey is not configured.");
                var deployment = configuration["AI:AzureOpenAI:DeploymentName"] ?? "gpt-4o";

                builder.AddAzureOpenAIChatCompletion(deployment, endpoint, apiKey);
                break;
            }

            case "GOOGLE":
            {
                var apiKey = configuration["AI:Google:ApiKey"]
                    ?? throw new InvalidOperationException("AI:Google:ApiKey is not configured.");
                var modelId = configuration["AI:Google:Model"] ?? "gemini-1.5-pro";

                builder.AddGoogleAIGeminiChatCompletion(modelId, apiKey);
                break;
            }

            // Default: OpenAI (also handles Anthropic via OpenAI-compatible endpoint)
            default:
            {
                var apiKey = configuration["AI:OpenAI:ApiKey"]
                    ?? throw new InvalidOperationException("AI:OpenAI:ApiKey is not configured.");
                var modelId = configuration["AI:OpenAI:Model"] ?? "gpt-4o";

                builder.AddOpenAIChatCompletion(modelId, apiKey);
                break;
            }
        }

        services.AddSingleton(builder.Build());
        return services;
    }
}
