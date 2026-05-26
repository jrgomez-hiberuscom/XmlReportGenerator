namespace XmlReportGenerator.Core.Interfaces;

/// <summary>
/// Exports a Crystal Reports template (.rpt) to HTML using a given XML datasource.
/// </summary>
public interface ICrystalReportExporter
{
    /// <summary>
    /// Loads the Crystal Reports template at <paramref name="rptFilePath"/>, injects
    /// <paramref name="xmlContent"/> as the XML datasource, and exports the report to HTML.
    /// </summary>
    /// <param name="rptFilePath">Absolute path to the .rpt template file.</param>
    /// <param name="xmlContent">The XML document to use as the report datasource.</param>
    /// <param name="outputFolderPath">The folder where the exported HTML file(s) will be written.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>The full path to the generated HTML file.</returns>
    Task<string> ExportToHtmlAsync(
        string rptFilePath,
        string xmlContent,
        string outputFolderPath,
        CancellationToken cancellationToken = default);
}
