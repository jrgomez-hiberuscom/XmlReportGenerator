using System.Text.RegularExpressions;
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
        // Minimize HTML to reduce token usage
        referenceHtml = MinifyHtml(referenceHtml);

        var promptPath = Path.Combine(
            AppContext.BaseDirectory, "Prompts", "instructionsBlazor.md");

        KernelFunction function;

        if (File.Exists(promptPath))
        {
            var promptMd = await File.ReadAllTextAsync(promptPath, cancellationToken);
            function = _kernel.CreateFunctionFromPrompt(promptMd);
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

    /// <summary>
    /// Minimizes HTML content by removing unnecessary whitespace, comments,
    /// and shortening CSS class names to reduce token count.
    /// </summary>
    private static string MinifyHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return html;

        // Remove HTML comments
        html = Regex.Replace(html, @"<!--.*?-->", "", RegexOptions.Singleline);

        // Remove line breaks and tabs
        html = html.Replace("\r\n", " ").Replace("\r", " ").Replace("\n", " ").Replace("\t", " ");

        // Collapse multiple spaces into one
        html = Regex.Replace(html, @" {2,}", " ");

        // Remove spaces between tags
        html = Regex.Replace(html, @">\s+<", "><");

        // Remove spaces around = in attributes
        html = Regex.Replace(html, @"\s*=\s*", "=");

        // Shorten CSS class names in <style> blocks and class attributes
        html = ShortenCssClassNames(html);

        return html.Trim();
    }

    /// <summary>
    /// Finds CSS class names defined in &lt;style&gt; blocks and replaces them
    /// with short aliases (c0, c1, c2...) both in the style definitions and in class attributes.
    /// </summary>
    private static string ShortenCssClassNames(string html)
    {
        // Extract all <style>...</style> blocks
        var styleMatch = Regex.Match(html, @"<style[^>]*>(.*?)</style>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        if (!styleMatch.Success)
            return html;

        var styleContent = styleMatch.Groups[1].Value;

        // Find all class selectors (.className) in the style block
        var classMatches = Regex.Matches(styleContent, @"\.([a-zA-Z_][\w-]*)");
        var classNames = classMatches
            .Cast<Match>()
            .Select(m => m.Groups[1].Value)
            .Distinct()
            .Where(name => name.Length > 3) // Only shorten names longer than 3 chars
            .ToList();

        if (classNames.Count == 0)
            return html;

        // Build mapping: original -> short name
        var mapping = new Dictionary<string, string>();
        for (int i = 0; i < classNames.Count; i++)
        {
            mapping[classNames[i]] = $"c{i}";
        }

        // Replace in style block (as selectors: .originalName)
        var newStyle = styleContent;
        foreach (var kvp in mapping.OrderByDescending(k => k.Key.Length))
        {
            newStyle = Regex.Replace(newStyle, @"\." + Regex.Escape(kvp.Key) + @"(?=[\s{,:])", "." + kvp.Value);
        }

        // Replace the style block in the HTML
        html = html.Remove(styleMatch.Groups[1].Index, styleMatch.Groups[1].Length)
                   .Insert(styleMatch.Groups[1].Index, newStyle);

        // Replace in class="..." attributes
        html = Regex.Replace(html, @"class=""([^""]+)""", m =>
        {
            var classes = m.Groups[1].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var replaced = classes.Select(c => mapping.TryGetValue(c, out var s) ? s : c);
            return $"class=\"{string.Join(' ', replaced)}\"";
        }, RegexOptions.IgnoreCase);

        return html;
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
