using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using XmlReportGenerator.Core.Interfaces;

namespace XmlReportGenerator.AI.Services;

/// <summary>
/// Generates Blazor components (.razor) from XML data and reference HTML using Semantic Kernel and a configured LLM.
/// </summary>
public class BlazorComponentGenerator : IBlazorComponentGenerator
{
    private readonly Kernel _kernel;
    private readonly ILogger<BlazorComponentGenerator> _logger;

    /// <summary>
    /// Initialises a new instance of <see cref="BlazorComponentGenerator"/>.
    /// </summary>
    public BlazorComponentGenerator(Kernel kernel, ILogger<BlazorComponentGenerator> logger)
    {
        _kernel = kernel;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> GenerateAsync(
        string xmlContent,
        string referenceHtml,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Loading GenerateBlazorComponent prompt template…");

        var promptTemplate = await LoadPromptTemplateAsync("GenerateBlazorComponent.yaml", cancellationToken)
            ?? FallbackBlazorPrompt;

        var function = _kernel.CreateFunctionFromPrompt(promptTemplate);

        var arguments = new KernelArguments
        {
            ["xml_content"] = xmlContent,
            ["reference_html"] = referenceHtml
        };

        var result = await _kernel.InvokeAsync(function, arguments, cancellationToken);
        var razorOutput = result.GetValue<string>() ?? string.Empty;

        razorOutput = StripMarkdownFences(razorOutput);

        _logger.LogDebug("LLM returned {Length} characters for the Blazor component.", razorOutput.Length);
        return razorOutput;
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

    private const string FallbackBlazorPrompt =
        "Generate a Blazor .razor component that accepts an XML string parameter named XmlData " +
        "and renders HTML equivalent to the reference HTML. Return only the .razor file content with no markdown.\n\n" +
        "XML:\n{{$xml_content}}\n\nReference HTML:\n{{$reference_html}}";
}
