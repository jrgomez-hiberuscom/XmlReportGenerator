using System.Text.RegularExpressions;
using XmlReportGenerator.Core.Interfaces;

namespace XmlReportGenerator.AI.Services;

/// <summary>
/// Generates a dynamic Blazor component (.razor) using <see cref="LiteLlmClient"/>.
/// The component reproduces the Crystal Reports HTML output from the provided XML data.
/// </summary>
public class BlazorComponentGenerator : IBlazorComponentGenerator
{
    private readonly LiteLlmClient _client;

    public BlazorComponentGenerator(LiteLlmClient client)
    {
        _client = client;
    }

    /// <inheritdoc />
    public async Task<string> GenerateComponentAsync(string xmlContent, string referenceHtml, CancellationToken cancellationToken = default)
    {
        referenceHtml = MinifyHtml(referenceHtml);

        const string system = "You are a Blazor component developer. Generate a single .razor component. Return ONLY the .razor file content, no explanations, no markdown code blocks.";

        var promptPath = Path.Combine(AppContext.BaseDirectory, "Prompts", "instructionsBlazor.md");
        string user;
        if (File.Exists(promptPath))
        {
            var instructions = await File.ReadAllTextAsync(promptPath, cancellationToken);
            user = $"{instructions}\n\nXML data:\n{xmlContent}\n\nReference HTML:\n{referenceHtml}";
        }
        else
        {
            user = $"""
                Given this XML data:
                {xmlContent}

                And this reference HTML produced by Crystal Reports:
                {referenceHtml}

                Generate a single Blazor component (.razor) that:
                - Reproduces the same visual layout as the reference HTML
                - Uses @code block to parse and bind the XML data
                - Uses standard Blazor/HTML markup (no external CSS frameworks required)
                - Is self-contained and compilable
                """;
        }

        var componentContent = await _client.ChatAsync(system, user, cancellationToken);

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

        html = Regex.Replace(html, @"<!--.*?-->", "", RegexOptions.Singleline);
        html = html.Replace("\r\n", " ").Replace("\r", " ").Replace("\n", " ").Replace("\t", " ");
        html = Regex.Replace(html, @" {2,}", " ");
        html = Regex.Replace(html, @">\s+<", "><");
        html = Regex.Replace(html, @"\s*=\s*", "=");
        html = ShortenCssClassNames(html);

        return html.Trim();
    }

    /// <summary>
    /// Finds CSS class names defined in &lt;style&gt; blocks and replaces them
    /// with short aliases (c0, c1, c2...) both in the style definitions and in class attributes.
    /// </summary>
    private static string ShortenCssClassNames(string html)
    {
        var styleMatch = Regex.Match(html, @"<style[^>]*>(.*?)</style>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        if (!styleMatch.Success)
            return html;

        var styleContent = styleMatch.Groups[1].Value;

        var classNames = Regex.Matches(styleContent, @"\.([a-zA-Z_][\w-]*)")
            .Cast<Match>()
            .Select(m => m.Groups[1].Value)
            .Distinct()
            .Where(name => name.Length > 3)
            .ToList();

        if (classNames.Count == 0)
            return html;

        var mapping = classNames
            .Select((name, i) => (name, alias: $"c{i}"))
            .ToDictionary(x => x.name, x => x.alias);

        var newStyle = styleContent;
        foreach (var kvp in mapping.OrderByDescending(k => k.Key.Length))
            newStyle = Regex.Replace(newStyle, @"\." + Regex.Escape(kvp.Key) + @"(?=[\s{,:])", "." + kvp.Value);

        html = html.Remove(styleMatch.Groups[1].Index, styleMatch.Groups[1].Length)
                   .Insert(styleMatch.Groups[1].Index, newStyle);

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
        content = Regex.Replace(content, @"^```[a-zA-Z]*\r?\n?", "", RegexOptions.Multiline);
        content = Regex.Replace(content, @"```\s*$", "", RegexOptions.Multiline);
        return content.Trim();
    }
}
