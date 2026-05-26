using Microsoft.Extensions.Logging;
using XmlReportGenerator.Core.Interfaces;
using XmlReportGenerator.Core.Models;

namespace XmlReportGenerator.Core.Orchestrator;

/// <summary>
/// Orchestrates the four-step report pipeline:
/// 1. AI XML Generation
/// 2. XML → JSON → Base64 → JSON wrapper
/// 3. Crystal Reports HTML export
/// 4. AI Blazor component generation
/// </summary>
public class ReportPipelineOrchestrator
{
    private readonly IXmlGeneratorService _xmlGenerator;
    private readonly IXsdValidator _xsdValidator;
    private readonly IXmlToJsonConverter _xmlToJsonConverter;
    private readonly IJsonEncoderService _jsonEncoder;
    private readonly ICrystalReportExporter _crystalReportExporter;
    private readonly IBlazorComponentGenerator _blazorComponentGenerator;
    private readonly ILogger<ReportPipelineOrchestrator> _logger;

    /// <summary>
    /// Initialises a new instance of <see cref="ReportPipelineOrchestrator"/>.
    /// </summary>
    public ReportPipelineOrchestrator(
        IXmlGeneratorService xmlGenerator,
        IXsdValidator xsdValidator,
        IXmlToJsonConverter xmlToJsonConverter,
        IJsonEncoderService jsonEncoder,
        ICrystalReportExporter crystalReportExporter,
        IBlazorComponentGenerator blazorComponentGenerator,
        ILogger<ReportPipelineOrchestrator> logger)
    {
        _xmlGenerator = xmlGenerator;
        _xsdValidator = xsdValidator;
        _xmlToJsonConverter = xmlToJsonConverter;
        _jsonEncoder = jsonEncoder;
        _crystalReportExporter = crystalReportExporter;
        _blazorComponentGenerator = blazorComponentGenerator;
        _logger = logger;
    }

    /// <summary>
    /// Executes all four pipeline steps and returns the aggregated result.
    /// </summary>
    /// <param name="input">The pipeline input containing file paths and configuration.</param>
    /// <param name="maxAiRetries">Maximum number of retries when the AI produces invalid XML.</param>
    /// <param name="cancellationToken">Propagates notification that the operation should be cancelled.</param>
    /// <returns>A <see cref="PipelineResult"/> containing all artefacts produced.</returns>
    public async Task<PipelineResult> RunAsync(
        PipelineInput input,
        int maxAiRetries = 3,
        CancellationToken cancellationToken = default)
    {
        var result = new PipelineResult();

        try
        {
            // ── Step 1: AI XML Generation ─────────────────────────────────────────
            _logger.LogInformation("Step 1: Generating XML with AI…");

            var xsdContent = await File.ReadAllTextAsync(input.XsdFilePath, cancellationToken);
            var markdownInstructions = await File.ReadAllTextAsync(input.MarkdownFilePath, cancellationToken);

            string generatedXml = string.Empty;
            var xmlValid = false;
            var attempts = 0;

            while (!xmlValid && attempts < maxAiRetries)
            {
                attempts++;
                _logger.LogInformation("  AI attempt {Attempt}/{Max}…", attempts, maxAiRetries);
                generatedXml = await _xmlGenerator.GenerateXmlAsync(xsdContent, markdownInstructions, cancellationToken);

                var validationErrors = _xsdValidator.GetValidationErrors(generatedXml, xsdContent);
                if (validationErrors.Count == 0)
                {
                    xmlValid = true;
                    _logger.LogInformation("  Generated XML is valid.");
                }
                else
                {
                    _logger.LogWarning("  Generated XML has {ErrorCount} validation error(s): {Errors}",
                        validationErrors.Count, string.Join("; ", validationErrors));
                }
            }

            if (!xmlValid)
            {
                _logger.LogWarning("  Proceeding with potentially invalid XML after {Max} retries.", maxAiRetries);
            }

            result.GeneratedXml = generatedXml;

            // ── Step 2: XML Pipeline ──────────────────────────────────────────────
            _logger.LogInformation("Step 2: Running XML pipeline (XML → JSON → Base64 → wrapper)…");

            result.JsonFromXml = _xmlToJsonConverter.Convert(generatedXml);
            result.Base64Json = _jsonEncoder.EncodeToBase64(result.JsonFromXml);
            result.JsonWrapper = _jsonEncoder.WrapInJson(result.Base64Json, new Dictionary<string, string>
            {
                ["xsdFile"] = Path.GetFileName(input.XsdFilePath),
                ["rptFile"] = Path.GetFileName(input.RptFilePath),
            });

            // Write JSON wrapper to disk
            Directory.CreateDirectory(input.OutputFolderPath);
            var jsonWrapperPath = Path.Combine(input.OutputFolderPath, "payload.json");
            await File.WriteAllTextAsync(jsonWrapperPath, result.JsonWrapper, cancellationToken);
            _logger.LogInformation("  JSON wrapper written to {Path}", jsonWrapperPath);

            // ── Step 3: Crystal Reports Export ────────────────────────────────────
            _logger.LogInformation("Step 3: Exporting Crystal Report to HTML…");

            var htmlPath = await _crystalReportExporter.ExportToHtmlAsync(
                input.RptFilePath, generatedXml, input.OutputFolderPath, cancellationToken);
            result.ReportHtml = await File.ReadAllTextAsync(htmlPath, cancellationToken);
            _logger.LogInformation("  Report exported to {Path}", htmlPath);

            // ── Step 4: Blazor Component Generation ──────────────────────────────
            _logger.LogInformation("Step 4: Generating Blazor component with AI…");

            result.BlazorComponent = await _blazorComponentGenerator.GenerateAsync(
                generatedXml, result.ReportHtml, cancellationToken);

            var razorPath = Path.Combine(input.OutputFolderPath, "GeneratedReport.razor");
            await File.WriteAllTextAsync(razorPath, result.BlazorComponent, cancellationToken);
            _logger.LogInformation("  Blazor component written to {Path}", razorPath);

            result.Success = true;
            _logger.LogInformation("Pipeline completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pipeline failed: {Message}", ex.Message);
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }
}
