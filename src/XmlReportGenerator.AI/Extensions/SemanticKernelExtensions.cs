using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace XmlReportGenerator.AI.Extensions;

/// <summary>
/// Extension methods for registering Semantic Kernel with the DI container.
/// </summary>
public static class SemanticKernelExtensions
{
    /// <summary>
    /// Registers the Semantic Kernel with the configured LLM provider from <paramref name="configuration"/>.
    /// </summary>
    /// <remarks>
    /// Reads the <c>AI:DefaultProvider</c> setting to determine which LLM to use.
    /// Supported values: <c>OpenAI</c>, <c>AzureOpenAI</c>.
    /// Google Gemini support can be enabled by installing
    /// <c>Microsoft.SemanticKernel.Connectors.Google</c> when publicly available.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddSemanticKernel(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var defaultProvider = configuration["AI:DefaultProvider"] ?? "OpenAI";

        var builder = Kernel.CreateBuilder();

        switch (defaultProvider.ToUpperInvariant())
        {
            case "AZUREOPENAI":
                var azureEndpoint = configuration["AI:AzureOpenAI:Endpoint"]
                    ?? throw new InvalidOperationException("AI:AzureOpenAI:Endpoint is not configured.");
                var azureApiKey = configuration["AI:AzureOpenAI:ApiKey"]
                    ?? throw new InvalidOperationException("AI:AzureOpenAI:ApiKey is not configured.");
                var azureDeployment = configuration["AI:AzureOpenAI:DeploymentName"] ?? "gpt-4o";

                builder.AddAzureOpenAIChatCompletion(
                    deploymentName: azureDeployment,
                    endpoint: azureEndpoint,
                    apiKey: azureApiKey);
                break;

            // Google Gemini support — requires Microsoft.SemanticKernel.Connectors.Google package.
            // Uncomment when the package is available and added to the project:
            //
            // case "GOOGLE":
            //     var googleApiKey = configuration["AI:Google:ApiKey"]
            //         ?? throw new InvalidOperationException("AI:Google:ApiKey is not configured.");
            //     var googleModel = configuration["AI:Google:Model"] ?? "gemini-1.5-pro";
            //     builder.AddGoogleAIGeminiChatCompletion(modelId: googleModel, apiKey: googleApiKey);
            //     break;

            case "OPENAI":
            default:
                var openAiApiKey = configuration["AI:OpenAI:ApiKey"]
                    ?? throw new InvalidOperationException("AI:OpenAI:ApiKey is not configured.");
                var openAiModel = configuration["AI:OpenAI:Model"] ?? "gpt-4o";

                builder.AddOpenAIChatCompletion(
                    modelId: openAiModel,
                    apiKey: openAiApiKey);
                break;
        }

        var kernel = builder.Build();
        services.AddSingleton(kernel);

        return services;
    }
}
