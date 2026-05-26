using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using XmlReportGenerator.Core.Interfaces;

namespace XmlReportGenerator.AI.Services;

/// <summary>
/// Generates fictitious but schema-valid XML documents using Semantic Kernel and a configured LLM.
/// </summary>
public class XmlGeneratorService : IXmlGeneratorService
{
    private readonly Kernel _kernel;
    private readonly ILogger<XmlGeneratorService> _logger;

    /// <summary>
    /// Initialises a new instance of <see cref="XmlGeneratorService"/>.
    /// </summary>
    public XmlGeneratorService(Kernel kernel, ILogger<XmlGeneratorService> logger)
    {
        _kernel = kernel;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> GenerateXmlAsync(
        string xsdContent,
        string markdownInstructions,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Loading GenerateXml prompt template…");

        var promptTemplate = await LoadPromptTemplateAsync("GenerateXml.yaml", cancellationToken)
            ?? FallbackXmlPrompt;

        var function = _kernel.CreateFunctionFromPrompt(promptTemplate);

        var arguments = new KernelArguments
        {
            ["xsd_content"] = xsdContent,
            ["markdown_instructions"] = markdownInstructions
        };

        var result = await _kernel.InvokeAsync(function, arguments, cancellationToken);
        var xmlOutput = result.GetValue<string>() ?? string.Empty;

        xmlOutput = StripMarkdownFences(xmlOutput);

        _logger.LogDebug("LLM returned {Length} characters of XML.", xmlOutput.Length);
        return xmlOutput;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Loads a prompt YAML file and extracts the template section (text after the <c>---</c> separator).
    /// Returns <c>null</c> if the file is not found.
    /// </summary>
    private static async Task<string?> LoadPromptTemplateAsync(string fileName, CancellationToken ct)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Prompts", fileName);
        if (!File.Exists(path)) return null;

        var content = await File.ReadAllTextAsync(path, ct);
        // SK YAML prompt format: metadata block, then '---' separator, then the prompt template
        var separatorIndex = content.IndexOf("\n---\n", StringComparison.Ordinal);
        return separatorIndex >= 0
            ? content[(separatorIndex + 5)..].Trim()
            : content.Trim();
    }

    private static string StripMarkdownFences(string text)
    {
        var lines = text.Split('\n');
        var result = new List<string>();

        foreach (var line in lines)
        {
            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal))
                continue;
            result.Add(line);
        }

        return string.Join('\n', result).Trim();
    }

    private const string FallbackXmlPrompt =
        "Generate a valid XML document that conforms to the following XSD schema. " +
        "Use entirely fictitious data. Return only the raw XML with no markdown fences.\n\n" +
        "XSD:\n{{$xsd_content}}\n\nInstructions:\n{{$markdown_instructions}}";
}
