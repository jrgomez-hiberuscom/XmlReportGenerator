using Microsoft.Extensions.Logging;
using System.Diagnostics;
using XmlReportGenerator.Core.Interfaces;

namespace XmlReportGenerator.Reports.Services;

/// <summary>
/// Exports Crystal Reports (.rpt) files to HTML by invoking the external .NET Framework 4.8 tool.
/// </summary>
/// <remarks>
/// <para>
/// Crystal Reports SDK requires .NET Framework 4.x and cannot run in .NET 9 directly.
/// This service delegates the export to <c>CrystalReportExporter.Tool.exe</c>, a .NET Framework 4.8
/// console application that performs the actual Crystal Reports rendering.
/// </para>
/// <para>
/// The tool path is resolved from the <c>CRYSTAL_EXPORTER_TOOL_PATH</c> environment variable,
/// or defaults to <c>tools\CrystalReportExporter.Tool\bin\Release\CrystalReportExporter.Tool.exe</c>
/// relative to the application base directory.
/// </para>
/// </remarks>
public class CrystalReportExporter : ICrystalReportExporter
{
    private readonly ILogger<CrystalReportExporter> _logger;

    public CrystalReportExporter(ILogger<CrystalReportExporter> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task ExportToHtmlAsync(
        string rptFilePath,
        string xmlContent,
        string xsdFilePath,
        string outputHtmlPath,
        CancellationToken cancellationToken = default)
    {
        var toolPath = ResolveToolPath();
        if (!File.Exists(toolPath))
        {
            throw new FileNotFoundException(
                $"Crystal Reports exporter tool not found at: {toolPath}. " +
                "Build the tools/CrystalReportExporter.Tool project targeting .NET Framework 4.8, " +
                "or set the CRYSTAL_EXPORTER_TOOL_PATH environment variable.");
        }

        // Write XML content to a temp file for the tool to read
        var tempXmlPath = Path.Combine(Path.GetTempPath(), $"cr_input_{Guid.NewGuid():N}.xml");
        try
        {
            await File.WriteAllTextAsync(tempXmlPath, xmlContent, cancellationToken);

            var arguments = $"--rpt \"{rptFilePath}\" --xml \"{tempXmlPath}\" --xsd \"{xsdFilePath}\" --output \"{outputHtmlPath}\"";

            _logger.LogInformation("Invoking Crystal Reports tool: {ToolPath} {Arguments}", toolPath, arguments);

            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = toolPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            process.Start();

            var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                _logger.LogError("Crystal Reports tool failed (exit code {ExitCode}): {StdErr}", process.ExitCode, stderr);
                throw new InvalidOperationException(
                    $"Crystal Reports export failed (exit code {process.ExitCode}): {stderr}");
            }

            _logger.LogInformation("Crystal Reports export completed: {Output}", stdout.Trim());
        }
        finally
        {
            try { File.Delete(tempXmlPath); } catch { }
        }
    }

    private static string ResolveToolPath()
    {
        // 1. Environment variable override
        var envPath = Environment.GetEnvironmentVariable("CRYSTAL_EXPORTER_TOOL_PATH");
        if (!string.IsNullOrEmpty(envPath))
            return envPath;

        // 2. Default: relative to app base directory
        return Path.Combine(
            AppContext.BaseDirectory,
            "tools",
            "CrystalReportExporter.Tool.exe");
    }
}
