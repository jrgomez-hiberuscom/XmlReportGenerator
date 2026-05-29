using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using XmlReportGenerator.Core.Interfaces;

namespace XmlReportGenerator.AI.Services;

/// <summary>
/// Generates fictitious but schema-valid XML documents using <see cref="LiteLlmClient"/>.
/// </summary>
public class XmlGeneratorService : IXmlGeneratorService
{
    private readonly LiteLlmClient _client;
    private readonly ILogger<XmlGeneratorService> _logger;

    private const int MaxXsdLength = 3300;
    private const int MaxInstructionsLength = 1500;

    public XmlGeneratorService(LiteLlmClient client, ILogger<XmlGeneratorService> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> GenerateXmlAsync(string xsdContent, string markdownInstructions, CancellationToken cancellationToken = default)
    {
        xsdContent = CompactXsd(xsdContent);
        if (xsdContent.Length > MaxXsdLength)
            //xsdContent = xsdContent[..MaxXsdLength] + "\n<!-- ...truncated -->";

        if (markdownInstructions.Length > MaxInstructionsLength)
            //markdownInstructions = markdownInstructions[..MaxInstructionsLength] + "\n[...truncated]";

        _logger.LogDebug("XSD input ({Length} chars)", xsdContent.Length);

        const string system = "You are an XML data generator. Generate a valid XML for the given schema. Return ONLY the XML document, no explanations, no markdown code blocks.";

        var promptPath = Path.Combine(AppContext.BaseDirectory, "Prompts", "GenerateXml.yaml");
        string user;
        if (File.Exists(promptPath))
        {
            // Extract the template text from the YAML and substitute variables manually
            var yaml = await File.ReadAllTextAsync(promptPath, cancellationToken);
            user = yaml
                .Replace("{{$xsdContent}}", xsdContent)
                .Replace("{{$markdownInstructions}}", markdownInstructions);
        }
        else
        {
            user = $"XSD:\n{xsdContent}\n\nInstructions:\n{markdownInstructions}";
        }

        var rawContent = await _client.ChatAsync(system, user, cancellationToken);

        _logger.LogDebug("LLM raw response ({Length} chars): {Raw}",
            rawContent.Length,
            rawContent.Length > 0 ? rawContent[..Math.Min(500, rawContent.Length)] : "(empty)");

        if (string.IsNullOrWhiteSpace(rawContent))
        {
            _logger.LogWarning("LLM returned an empty response for XML generation.");
            return string.Empty;
        }

        return StripMarkdownCodeFences(rawContent).Trim();
    }

    /// <summary>
    /// Removes XML comments, msprop:* attributes and collapses whitespace to reduce token usage.
    /// </summary>
    private static string CompactXsd(string xsd)
    {
        xsd = Regex.Replace(xsd, @"<!--.*?-->", "", RegexOptions.Singleline);
        xsd = Regex.Replace(xsd, @"\s+msprop:\w+=""[^""]*""", "");
        var lines = xsd.Split('\n')
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrEmpty(l));
        return string.Join('\n', lines);
    }

    private static string StripMarkdownCodeFences(string content)
    {
        content = Regex.Replace(content, @"^```[a-zA-Z]*\r?\n?", "", RegexOptions.Multiline);
        content = Regex.Replace(content, @"```\s*$", "", RegexOptions.Multiline);
        return content.Trim();
    }
}