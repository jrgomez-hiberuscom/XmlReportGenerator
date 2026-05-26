using Microsoft.SemanticKernel;
using XmlReportGenerator.Core.Interfaces;

namespace XmlReportGenerator.AI.Services;

/// <summary>
/// Generates fictitious but schema-valid XML documents using Semantic Kernel and an LLM.
/// </summary>
public class XmlGeneratorService : IXmlGeneratorService
{
    private readonly Kernel _kernel;

    /// <summary>
    /// Initializes a new instance of <see cref="XmlGeneratorService"/>.
    /// </summary>
    /// <param name="kernel">The configured Semantic Kernel instance.</param>
    public XmlGeneratorService(Kernel kernel)
    {
        _kernel = kernel;
    }

    /// <inheritdoc />
    public async Task<string> GenerateXmlAsync(string xsdContent, string markdownInstructions, CancellationToken cancellationToken = default)
    {
        var promptPath = Path.Combine(
            AppContext.BaseDirectory, "Prompts", "GenerateXml.yaml");

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
                You are an XML data generator.

                Given the following XSD schema:
                {{$xsdContent}}

                And the following instructions:
                {{$markdownInstructions}}

                Generate a single valid XML document that:
                - Conforms strictly to the XSD schema
                - Contains realistic-looking but entirely fictitious data
                - Includes all required elements and attributes
                - Returns ONLY the XML document, no explanations or markdown code blocks
                """;

            function = _kernel.CreateFunctionFromPrompt(inlinePrompt);
        }

        var arguments = new KernelArguments
        {
            ["xsdContent"] = xsdContent,
            ["markdownInstructions"] = markdownInstructions,
        };

        var response = await _kernel.InvokeAsync(function, arguments, cancellationToken);
        var xmlContent = response.GetValue<string>() ?? string.Empty;

        // Strip markdown code fences if the LLM wrapped the response
        xmlContent = StripMarkdownCodeFences(xmlContent);

        return xmlContent.Trim();
    }

    private static string StripMarkdownCodeFences(string content)
    {
        if (content.StartsWith("```xml", StringComparison.OrdinalIgnoreCase))
            content = content[6..];
        else if (content.StartsWith("```", StringComparison.Ordinal))
            content = content[3..];

        if (content.EndsWith("```", StringComparison.Ordinal))
            content = content[..^3];

        return content.Trim();
    }
}
