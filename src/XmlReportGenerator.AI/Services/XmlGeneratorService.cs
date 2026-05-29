using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using XmlReportGenerator.Core.Interfaces;

namespace XmlReportGenerator.AI.Services;

/// <summary>
/// Generates fictitious but schema-valid XML documents using Semantic Kernel and an LLM.
/// When a <see cref="LiteLlmClient"/> is provided it is used directly (bypassing SK) to avoid
/// issues with models that return extra fields such as <c>reasoning_content</c>.
/// </summary>
public class XmlGeneratorService : IXmlGeneratorService
{
    private readonly Kernel _kernel;
    private readonly LiteLlmClient? _liteLlm;
    private readonly ILogger<XmlGeneratorService> _logger;

    private const int MaxXsdLength = 3300;
    private const int MaxInstructionsLength = 1500;

    public XmlGeneratorService(Kernel kernel, ILogger<XmlGeneratorService> logger, LiteLlmClient? liteLlm = null)
    {
        _kernel = kernel;
        _logger = logger;
        _liteLlm = liteLlm;
    }

    /// <inheritdoc />
    public async Task<string> GenerateXmlAsync(string xsdContent, string markdownInstructions, CancellationToken cancellationToken = default)
    {
        // Compact inputs to stay within token limits
        xsdContent = CompactXsd(xsdContent);

        if (markdownInstructions.Length > MaxInstructionsLength)
            markdownInstructions = markdownInstructions[..MaxInstructionsLength] + "\n[...truncated]";

        _logger.LogDebug("XSD input ({Length} chars): {Xsd}", xsdContent.Length, xsdContent[..Math.Min(200, xsdContent.Length)]);

        string rawContent;

        if (_liteLlm is not null)
        {
            // Direct HTTP call — avoids SK parsing issues with reasoning models
            const string system = "You are an XML data generator. Generate a valid XML for the given schema. Return ONLY the XML document, no explanations, no markdown code blocks.";
            var user = $"XSD:\n{xsdContent}\n\nInstructions:\n{markdownInstructions}";
            rawContent = await _liteLlm.ChatAsync(system, user, cancellationToken);
        }
        else
        {
            rawContent = await InvokeViaSKAsync(xsdContent, markdownInstructions, cancellationToken);
        }

        _logger.LogDebug("LLM raw response ({Length} chars): {Raw}",
            rawContent.Length,
            rawContent.Length > 0 ? rawContent[..Math.Min(500, rawContent.Length)] : "(empty)");

        if (string.IsNullOrWhiteSpace(rawContent))
        {
            _logger.LogWarning("LLM returned an empty response for XML generation.");
            return string.Empty;
        }

        var xmlContent = StripMarkdownCodeFences(rawContent);
        _logger.LogDebug("XML after stripping fences ({Length} chars)", xmlContent.Length);

        return xmlContent.Trim();
    }

    private async Task<string> InvokeViaSKAsync(string xsdContent, string markdownInstructions, CancellationToken cancellationToken)
    {
        var promptPath = Path.Combine(AppContext.BaseDirectory, "Prompts", "GenerateXml.yaml");

        KernelFunction function;

        if (File.Exists(promptPath))
        {
            var promptYaml = await File.ReadAllTextAsync(promptPath, cancellationToken);
            function = KernelFunctionYaml.FromPromptYaml(promptYaml);
        }
        else
        {
            const string inlinePrompt = """
                You are an XML data generator. Generate a valid XML for this schema. Return ONLY XML.

                XSD:
                {{$xsdContent}}

                Instructions:
                {{$markdownInstructions}}
                """;
            function = _kernel.CreateFunctionFromPrompt(inlinePrompt);
        }

        var arguments = new KernelArguments
        {
            ["xsdContent"] = xsdContent,
            ["markdownInstructions"] = markdownInstructions,
        };

        var response = await _kernel.InvokeAsync(function, arguments, cancellationToken);
        return response.GetValue<string>() ?? string.Empty;
    }

    /// <summary>
    /// Removes XML comments and collapses whitespace to reduce token usage.
    /// </summary>
    private static string CompactXsd(string xsd)
    {
        // Remove XML comments
        xsd = Regex.Replace(xsd, @"<!--.*?-->", "", RegexOptions.Singleline);
        // Remove msprop:* attributes (e.g. msprop:SomeProp="value")
        xsd = Regex.Replace(xsd, @"\s+msprop:\w+=""[^""]*""", "");
        var lines = xsd.Split('\n')
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrEmpty(l));
        return string.Join('\n', lines);
    }

    /// <summary>
    /// Strips markdown code fences (```xml ... ```) from LLM responses.
    /// </summary>
    private static string StripMarkdownCodeFences(string content)
    {
        content = Regex.Replace(content, @"^```[a-zA-Z]*\r?\n?", "", RegexOptions.Multiline);
        content = Regex.Replace(content, @"```\s*$", "", RegexOptions.Multiline);
        return content.Trim();
    }
}