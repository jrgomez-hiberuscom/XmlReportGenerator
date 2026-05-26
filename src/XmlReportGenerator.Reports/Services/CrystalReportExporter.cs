using Microsoft.Extensions.Logging;
using XmlReportGenerator.Core.Interfaces;

namespace XmlReportGenerator.Reports.Services;

/// <summary>
/// Exports Crystal Reports templates (.rpt) to HTML using an XML datasource.
/// </summary>
/// <remarks>
/// This class requires the SAP Crystal Reports Runtime to be installed and the
/// <c>CRYSTAL_REPORTS</c> preprocessor symbol to be defined at compile time.
/// Without those, a stub implementation is used that writes a placeholder HTML file.
/// </remarks>
public class CrystalReportExporter : ICrystalReportExporter
{
    private readonly ILogger<CrystalReportExporter> _logger;

    /// <summary>
    /// Initialises a new instance of <see cref="CrystalReportExporter"/>.
    /// </summary>
    public CrystalReportExporter(ILogger<CrystalReportExporter> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> ExportToHtmlAsync(
        string rptFilePath,
        string xmlContent,
        string outputFolderPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rptFilePath))
            throw new ArgumentException("RPT file path must not be null or empty.", nameof(rptFilePath));
        if (!File.Exists(rptFilePath))
            throw new FileNotFoundException($"Crystal Reports template not found: {rptFilePath}", rptFilePath);
        if (string.IsNullOrWhiteSpace(xmlContent))
            throw new ArgumentException("XML content must not be null or empty.", nameof(xmlContent));

        Directory.CreateDirectory(outputFolderPath);
        var outputPath = Path.Combine(outputFolderPath, "report.html");

#if CRYSTAL_REPORTS
        _logger.LogInformation("Exporting Crystal Report '{RptPath}' to HTML…", rptFilePath);

        // ── Real Crystal Reports implementation ───────────────────────────────
        // Write XML to a temporary file so Crystal Reports can read it as datasource
        var tempXmlPath = Path.Combine(Path.GetTempPath(), $"report_data_{Guid.NewGuid():N}.xml");
        try
        {
            await File.WriteAllTextAsync(tempXmlPath, xmlContent, cancellationToken);

            using var report = new CrystalDecisions.CrystalReports.Engine.ReportDocument();
            report.Load(rptFilePath);

            // Set the XML file as the datasource
            report.SetDataSource(tempXmlPath);

            // Configure HTML export options
            var htmlOptions = new CrystalDecisions.Shared.HTMLFormatOptions
            {
                HTMLBaseFolderName = outputFolderPath,
                HTMLFileName = "report.html",
                HTMLEnableSeparatedPages = false,
                UsePageRange = false
            };

            var exportOptions = new CrystalDecisions.Shared.ExportOptions
            {
                ExportFormatType = CrystalDecisions.Shared.ExportFormatType.HTML40,
                ExportDestinationType = CrystalDecisions.Shared.ExportDestinationType.DiskFile,
                ExportFormatOptions = htmlOptions,
                ExportDestinationOptions = new CrystalDecisions.Shared.DiskFileDestinationOptions
                {
                    DiskFileName = outputPath
                }
            };

            report.Export(exportOptions);
            _logger.LogInformation("Crystal Report exported to '{OutputPath}'", outputPath);
        }
        finally
        {
            if (File.Exists(tempXmlPath))
                File.Delete(tempXmlPath);
        }
#else
        // ── Stub implementation (no Crystal Reports runtime) ──────────────────
        _logger.LogWarning(
            "Crystal Reports runtime is not available (CRYSTAL_REPORTS symbol not defined). " +
            "Writing placeholder HTML to '{OutputPath}'.", outputPath);

        var placeholderHtml = $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta charset="UTF-8" />
              <title>Report Placeholder</title>
              <style>
                body { font-family: sans-serif; padding: 2rem; }
                .notice { background: #fff3cd; border: 1px solid #ffc107; padding: 1rem; border-radius: 4px; }
              </style>
            </head>
            <body>
              <div class="notice">
                <h2>Crystal Reports Runtime Not Available</h2>
                <p>This is a stub HTML file. To generate the real report output, install the
                   <strong>SAP Crystal Reports Runtime</strong> and rebuild with the
                   <code>CRYSTAL_REPORTS</code> constant defined.</p>
                <p><strong>Template:</strong> {{Path.GetFileName(rptFilePath)}}</p>
                <p><strong>Generated:</strong> {{DateTime.UtcNow:O}}</p>
              </div>
            </body>
            </html>
            """;

        await File.WriteAllTextAsync(outputPath, placeholderHtml, cancellationToken);
#endif

        return outputPath;
    }
}
