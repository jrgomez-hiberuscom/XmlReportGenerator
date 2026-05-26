namespace XmlReportGenerator.Core.Models;

/// <summary>
/// Holds the results produced by each step of the report generation pipeline.
/// </summary>
public class PipelineResult
{
    /// <summary>Gets or sets the AI-generated XML content (Step 1).</summary>
    public string GeneratedXml { get; set; } = string.Empty;

    /// <summary>Gets or sets the JSON representation of the generated XML (Step 2).</summary>
    public string XmlAsJson { get; set; } = string.Empty;

    /// <summary>Gets or sets the JSON wrapper payload containing the Base64-encoded JSON (Step 2).</summary>
    public JsonWrapperModel? JsonWrapper { get; set; }

    /// <summary>Gets or sets the path to the exported HTML report file (Step 3).</summary>
    public string HtmlReportPath { get; set; } = string.Empty;

    /// <summary>Gets or sets the content of the generated Blazor component (Step 4).</summary>
    public string BlazorComponentContent { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the pipeline completed successfully.</summary>
    public bool IsSuccess { get; set; }

    /// <summary>Gets or sets any error messages encountered during the pipeline.</summary>
    public List<string> Errors { get; set; } = new List<string>();
}
