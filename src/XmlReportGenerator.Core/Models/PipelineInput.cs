namespace XmlReportGenerator.Core.Models;

/// <summary>
/// Represents the input parameters for the report generation pipeline.
/// </summary>
public class PipelineInput
{
    /// <summary>Gets or sets the full path to the Crystal Reports .rpt file.</summary>
    public string RptFilePath { get; set; } = string.Empty;

    /// <summary>Gets or sets the full path to the XSD schema file.</summary>
    public string XsdFilePath { get; set; } = string.Empty;

    /// <summary>Gets or sets the full path to the Markdown instructions file.</summary>
    public string MarkdownFilePath { get; set; } = string.Empty;

    /// <summary>Gets or sets the output directory where generated files will be saved.</summary>
    public string OutputDirectory { get; set; } = string.Empty;
}
