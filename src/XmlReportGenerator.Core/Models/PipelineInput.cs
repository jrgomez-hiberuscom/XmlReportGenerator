namespace XmlReportGenerator.Core.Models;

/// <summary>
/// Represents the input required to start the report pipeline.
/// </summary>
public class PipelineInput
{
    /// <summary>Gets or sets the absolute path to the folder containing the input files.</summary>
    public required string InputFolderPath { get; set; }

    /// <summary>Gets or sets the path to the XSD schema file.</summary>
    public required string XsdFilePath { get; set; }

    /// <summary>Gets or sets the path to the Crystal Reports template (.rpt) file.</summary>
    public required string RptFilePath { get; set; }

    /// <summary>Gets or sets the path to the Markdown instructions file.</summary>
    public required string MarkdownFilePath { get; set; }

    /// <summary>Gets or sets the output directory where generated files will be written.</summary>
    public required string OutputFolderPath { get; set; }
}
