namespace XmlReportGenerator.Core.Models;

/// <summary>
/// Represents the output produced after the pipeline completes all four steps.
/// </summary>
public class PipelineResult
{
    /// <summary>Gets or sets the AI-generated XML document content.</summary>
    public string GeneratedXml { get; set; } = string.Empty;

    /// <summary>Gets or sets the JSON representation of the generated XML.</summary>
    public string JsonFromXml { get; set; } = string.Empty;

    /// <summary>Gets or sets the Base64-encoded JSON.</summary>
    public string Base64Json { get; set; } = string.Empty;

    /// <summary>Gets or sets the final JSON wrapper that embeds the Base64-encoded payload.</summary>
    public string JsonWrapper { get; set; } = string.Empty;

    /// <summary>Gets or sets the HTML output generated from Crystal Reports.</summary>
    public string ReportHtml { get; set; } = string.Empty;

    /// <summary>Gets or sets the generated Blazor component (.razor) content.</summary>
    public string BlazorComponent { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the entire pipeline succeeded.</summary>
    public bool Success { get; set; }

    /// <summary>Gets or sets any error message produced during the pipeline run.</summary>
    public string? ErrorMessage { get; set; }
}
