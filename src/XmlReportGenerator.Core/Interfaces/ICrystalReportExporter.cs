namespace XmlReportGenerator.Core.Interfaces;

/// <summary>
/// Exports a Crystal Reports (.rpt) file to HTML using the provided XML as the datasource.
/// </summary>
public interface ICrystalReportExporter
{
    /// <summary>
    /// Loads the specified Crystal Reports file, injects the XML content as the datasource,
    /// and exports the report to HTML.
    /// </summary>
    /// <param name="rptFilePath">The full path to the .rpt Crystal Reports file.</param>
    /// <param name="xmlContent">The XML content to use as the report datasource.</param>
    /// <param name="outputHtmlPath">The destination path for the exported HTML file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ExportToHtmlAsync(string rptFilePath, string xmlContent, string xsdFilePath, string outputHtmlPath, CancellationToken cancellationToken = default);
}
