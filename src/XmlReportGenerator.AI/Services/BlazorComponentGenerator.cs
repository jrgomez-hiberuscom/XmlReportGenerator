using Microsoft.SemanticKernel;
using XmlReportGenerator.Core.Interfaces;

namespace XmlReportGenerator.AI.Services;

/// <summary>
/// Generates a dynamic Blazor component (.razor) using Semantic Kernel and an LLM.
/// The component reproduces the Crystal Reports HTML output from the provided XML data.
/// </summary>
public class BlazorComponentGenerator : IBlazorComponentGenerator
{
    private readonly Kernel _kernel;

    /// <summary>
    /// Initializes a new instance of <see cref="BlazorComponentGenerator"/>.
    /// </summary>
    /// <param name="kernel">The configured Semantic Kernel instance.</param>
    public BlazorComponentGenerator(Kernel kernel)
    {
        _kernel = kernel;
    }

    /// <inheritdoc />
    public async Task<string> GenerateComponentAsync(string xmlContent, string referenceHtml, CancellationToken cancellationToken = default)
    {
        var promptPath = Path.Combine(
            AppContext.BaseDirectory, "Prompts", "GenerateBlazorComponent.yaml");

        KernelFunction function;

        if (File.Exists(promptPath))
        {
            var promptYaml = await File.ReadAllTextAsync(promptPath, cancellationToken);
            function = KernelFunctionYaml.FromPromptYaml(promptYaml);
        }
        else
        {
            // Fallback inline prompt when YAML file is not available
            const string inlinePrompt = """
                You are a Blazor component developer.

                Given this XML data:
                {{$xmlContent}}

                And this reference HTML produced by Crystal Reports:
                {{$referenceHtml}}

                Generate a single Blazor component (.razor) that:
                - Reproduces the same visual layout as the reference HTML
                - Uses @code block to parse and bind the XML data
                - Uses standard Blazor/HTML markup (no external CSS frameworks required)
                - Is self-contained and compilable
                - Returns ONLY the .razor file content, no explanations
                """;

            function = _kernel.CreateFunctionFromPrompt(inlinePrompt);
        }

        var arguments = new KernelArguments
        {
            ["xmlContent"] = xmlContent,
            ["referenceHtml"] = referenceHtml,
        };

        var response = await _kernel.InvokeAsync(function, arguments, cancellationToken);
        var componentContent = response.GetValue<string>() ?? string.Empty;

        // Strip markdown code fences if the LLM wrapped the response
        componentContent = StripMarkdownCodeFences(componentContent);

        return componentContent.Trim();
    }

    private static string StripMarkdownCodeFences(string content)
    {
        if (content.StartsWith("```razor", StringComparison.OrdinalIgnoreCase))
            content = content[8..];
        else if (content.StartsWith("```", StringComparison.Ordinal))
            content = content[3..];

        if (content.EndsWith("```", StringComparison.Ordinal))
            content = content[..^3];

        return content.Trim();
    }
}
