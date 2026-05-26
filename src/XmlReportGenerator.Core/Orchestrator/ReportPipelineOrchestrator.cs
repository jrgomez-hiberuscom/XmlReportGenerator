using Microsoft.Extensions.Logging;
using XmlReportGenerator.Core.Interfaces;
using XmlReportGenerator.Core.Models;

namespace XmlReportGenerator.Core.Orchestrator;

/// <summary>
/// Orchestrates the four-step report generation pipeline:
/// <list type="number">
///   <item><description>AI XML Generation — generates fictitious but schema-valid XML.</description></item>
///   <item><description>XML Pipeline — converts XML → JSON → Base64 → JSON wrapper.</description></item>
///   <item><description>Crystal Reports Export — exports the .rpt to HTML using the generated XML.</description></item>
///   <item><description>Blazor Component Generator — generates a .razor component reproducing the HTML.</description></item>
/// </list>
/// </summary>
public class ReportPipelineOrchestrator
{
    private readonly IXmlGeneratorService _xmlGeneratorService;
    private readonly IXsdValidator _xsdValidator;
    private readonly IXmlToJsonConverter _xmlToJsonConverter;
    private readonly IJsonEncoderService _jsonEncoderService;
    private readonly ICrystalReportExporter _crystalReportExporter;
    private readonly IBlazorComponentGenerator _blazorComponentGenerator;
    private readonly ILogger<ReportPipelineOrchestrator> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="ReportPipelineOrchestrator"/>.
    /// </summary>
    public ReportPipelineOrchestrator(
        IXmlGeneratorService xmlGeneratorService,
        IXsdValidator xsdValidator,
        IXmlToJsonConverter xmlToJsonConverter,
        IJsonEncoderService jsonEncoderService,
        ICrystalReportExporter crystalReportExporter,
        IBlazorComponentGenerator blazorComponentGenerator,
        ILogger<ReportPipelineOrchestrator> logger)
    {
        _xmlGeneratorService = xmlGeneratorService;
        _xsdValidator = xsdValidator;
        _xmlToJsonConverter = xmlToJsonConverter;
        _jsonEncoderService = jsonEncoderService;
        _crystalReportExporter = crystalReportExporter;
        _blazorComponentGenerator = blazorComponentGenerator;
        _logger = logger;
    }

    /// <summary>
    /// Runs the full pipeline using the files found in the provided <paramref name="input"/>.
    /// </summary>
    /// <param name="input">The pipeline input specifying file paths and output directory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="PipelineResult"/> with the outputs of each step.</returns>
    public async Task<PipelineResult> RunAsync(PipelineInput input, CancellationToken cancellationToken = default)
    {
        var result = new PipelineResult();

        try
        {
            // ── Step 1: AI XML Generation ─────────────────────────────────────────────
            _logger.LogInformation("Step 1: Generating XML with AI...");
            var xsdContent = await File.ReadAllTextAsync(input.XsdFilePath, cancellationToken);
            var mdContent = await File.ReadAllTextAsync(input.MarkdownFilePath, cancellationToken);

            result.GeneratedXml = await _xmlGeneratorService.GenerateXmlAsync(xsdContent, mdContent, cancellationToken);

            // Validate the generated XML against the schema
            var (isValid, errors) = _xsdValidator.Validate(result.GeneratedXml, xsdContent);
            if (!isValid)
            {
                _logger.LogWarning("Generated XML does not fully validate against XSD: {Errors}", string.Join("; ", errors));
                result.Errors.AddRange(errors);
            }

            var xmlOutputPath = Path.Combine(input.OutputDirectory, "generated.xml");
            Directory.CreateDirectory(input.OutputDirectory);
            await File.WriteAllTextAsync(xmlOutputPath, result.GeneratedXml, cancellationToken);
            _logger.LogInformation("Step 1 complete. XML written to {Path}", xmlOutputPath);

            // ── Step 2: XML Pipeline ──────────────────────────────────────────────────
            _logger.LogInformation("Step 2: Running XML pipeline (XML → JSON → Base64 → wrapper)...");
            result.XmlAsJson = _xmlToJsonConverter.Convert(result.GeneratedXml);
            result.JsonWrapper = _jsonEncoderService.Encode(result.XmlAsJson);

            var jsonOutputPath = Path.Combine(input.OutputDirectory, "payload.json");
            await File.WriteAllTextAsync(jsonOutputPath, Newtonsoft.Json.JsonConvert.SerializeObject(result.JsonWrapper, Newtonsoft.Json.Formatting.Indented), cancellationToken);
            _logger.LogInformation("Step 2 complete. Wrapper JSON written to {Path}", jsonOutputPath);

            // ── Step 3: Crystal Reports Export ────────────────────────────────────────
            _logger.LogInformation("Step 3: Exporting Crystal Report to HTML...");
            var htmlOutputPath = Path.Combine(input.OutputDirectory, "report.html");
            await _crystalReportExporter.ExportToHtmlAsync(input.RptFilePath, result.GeneratedXml, htmlOutputPath, cancellationToken);
            result.HtmlReportPath = htmlOutputPath;
            _logger.LogInformation("Step 3 complete. HTML report written to {Path}", htmlOutputPath);

            // ── Step 4: Blazor Component Generator ────────────────────────────────────
            _logger.LogInformation("Step 4: Generating Blazor component with AI...");
            var referenceHtml = await File.ReadAllTextAsync(htmlOutputPath, cancellationToken);
            result.BlazorComponentContent = await _blazorComponentGenerator.GenerateComponentAsync(result.GeneratedXml, referenceHtml, cancellationToken);

            var razorOutputPath = Path.Combine(input.OutputDirectory, "ReportComponent.razor");
            await File.WriteAllTextAsync(razorOutputPath, result.BlazorComponentContent, cancellationToken);
            _logger.LogInformation("Step 4 complete. Blazor component written to {Path}", razorOutputPath);

            result.IsSuccess = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pipeline failed: {Message}", ex.Message);
            result.Errors.Add(ex.Message);
            result.IsSuccess = false;
        }

        return result;
    }
}
