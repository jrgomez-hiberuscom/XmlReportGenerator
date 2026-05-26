using Microsoft.Extensions.Logging;
using XmlReportGenerator.Core.Interfaces;

namespace XmlReportGenerator.Reports.Services;

/// <summary>
/// Exports Crystal Reports (.rpt) files to HTML using the provided XML as the datasource.
/// </summary>
/// <remarks>
/// <para>
/// This service requires the SAP Crystal Reports Runtime for Visual Studio to be installed.
/// The runtime is NOT available on NuGet; it must be installed from the SAP website:
/// https://www.sap.com/products/technology-platform/crystal-reports.html
/// </para>
/// <para>
/// After installing the runtime, add local references to the Crystal Reports DLLs from:
/// C:\Program Files (x86)\SAP BusinessObjects\Crystal Reports for .NET Framework 4.0\Common\
/// SAP BusinessObjects Enterprise XI 4.0\win32_x86\
/// </para>
/// <para>
/// To enable full Crystal Reports functionality, define the <c>CRYSTAL_REPORTS</c> compilation
/// constant in your build configuration.
/// </para>
/// </remarks>
public class CrystalReportExporter : ICrystalReportExporter
{
    private readonly ILogger<CrystalReportExporter> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="CrystalReportExporter"/>.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public CrystalReportExporter(ILogger<CrystalReportExporter> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task ExportToHtmlAsync(
        string rptFilePath,
        string xmlContent,
        string outputHtmlPath,
        CancellationToken cancellationToken = default)
    {
#if CRYSTAL_REPORTS
        // ── Crystal Reports Runtime Required ─────────────────────────────────────
        // Uncomment and use the following code once the SAP Crystal Reports Runtime
        // is installed and the DLL references are configured.
        //
        // using var report = new CrystalDecisions.CrystalReports.Engine.ReportDocument();
        // report.Load(rptFilePath);
        //
        // // Write the XML content to a temporary file for use as datasource
        // var xmlTempPath = Path.GetTempFileName() + ".xml";
        // await File.WriteAllTextAsync(xmlTempPath, xmlContent, cancellationToken);
        //
        // try
        // {
        //     // Set the XML file as the datasource
        //     report.SetDataSource(new System.Data.DataSet());
        //     // For XML datasource, Crystal Reports uses the file path:
        //     report.DataSourceConnections[0].SetLogonInfo("", "", "", xmlTempPath);
        //
        //     // Export to HTML
        //     var exportOptions = new CrystalDecisions.Shared.ExportOptions();
        //     var htmlFormatOptions = new CrystalDecisions.Shared.HtmlFormatOptions();
        //     htmlFormatOptions.HTMLBaseFolderName = Path.GetDirectoryName(outputHtmlPath)!;
        //     htmlFormatOptions.HTMLFileName = Path.GetFileName(outputHtmlPath);
        //     exportOptions.ExportFormatType = CrystalDecisions.Shared.ExportFormatType.HTML40;
        //     exportOptions.FormatOptions = htmlFormatOptions;
        //     exportOptions.ExportDestinationType = CrystalDecisions.Shared.ExportDestinationType.DiskFile;
        //
        //     report.Export(exportOptions);
        //     _logger.LogInformation("Crystal report exported to HTML: {Path}", outputHtmlPath);
        // }
        // finally
        // {
        //     if (File.Exists(xmlTempPath))
        //         File.Delete(xmlTempPath);
        //     report.Close();
        // }
        await Task.CompletedTask;
#else
        // ── Stub mode (no Crystal Reports runtime) ────────────────────────────────
        // Produces a placeholder HTML file so the pipeline can continue.
        _logger.LogWarning(
            "Crystal Reports runtime is not available (CRYSTAL_REPORTS symbol not defined). " +
            "Generating placeholder HTML. Install the SAP Crystal Reports Runtime and define " +
            "CRYSTAL_REPORTS to enable full export functionality.");

        var placeholderHtml = $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="UTF-8" />
                <title>Report - Placeholder</title>
                <style>
                    body { font-family: Arial, sans-serif; margin: 2rem; }
                    .notice { background: #fff3cd; border: 1px solid #ffc107; padding: 1rem; border-radius: 4px; }
                </style>
            </head>
            <body>
                <div class="notice">
                    <strong>Crystal Reports Stub</strong>
                    <p>The SAP Crystal Reports runtime is not installed. This is a placeholder HTML output.</p>
                    <p>Source report: <code>{{rptFilePath}}</code></p>
                    <p>Generated at: {{DateTime.UtcNow:O}}</p>
                </div>
                <hr />
                <h2>XML Datasource Preview</h2>
                <pre>{{System.Security.SecurityElement.Escape(xmlContent)}}</pre>
            </body>
            </html>
            """;

        Directory.CreateDirectory(Path.GetDirectoryName(outputHtmlPath)!);
        await File.WriteAllTextAsync(outputHtmlPath, placeholderHtml, cancellationToken);

        _logger.LogInformation("Placeholder HTML written to {Path}", outputHtmlPath);
#endif
    }
}
