using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using XmlReportGenerator.AI.Extensions;
using XmlReportGenerator.AI.Services;
using XmlReportGenerator.Blazor.Services;
using XmlReportGenerator.Core.Interfaces;
using XmlReportGenerator.Core.Models;
using XmlReportGenerator.Core.Orchestrator;
using XmlReportGenerator.Core.Services;
using XmlReportGenerator.Reports.Services;

// ── Parse CLI arguments ───────────────────────────────────────────────────────
string? inputFolder = null;
for (var i = 0; i < args.Length - 1; i++)
{
    if (args[i] is "--input" or "-i")
    {
        inputFolder = args[i + 1];
        break;
    }
}

if (string.IsNullOrWhiteSpace(inputFolder))
{
    AnsiConsole.MarkupLine("[red]Usage:[/] dotnet run -- --input <folder-path>");
    return 1;
}

inputFolder = Path.GetFullPath(inputFolder);

if (!Directory.Exists(inputFolder))
{
    AnsiConsole.MarkupLine($"[red]Error:[/] Input folder not found: {inputFolder}");
    return 1;
}

// ── Locate required files ─────────────────────────────────────────────────────
var xsdFiles = Directory.GetFiles(inputFolder, "*.xsd");
var rptFiles = Directory.GetFiles(inputFolder, "*.rpt");
var mdFiles = Directory.GetFiles(inputFolder, "*.md");

if (xsdFiles.Length == 0)
{
    AnsiConsole.MarkupLine("[red]Error:[/] No .xsd file found in the input folder.");
    return 1;
}
if (rptFiles.Length == 0)
{
    AnsiConsole.MarkupLine("[red]Error:[/] No .rpt file found in the input folder.");
    return 1;
}
if (mdFiles.Length == 0)
{
    AnsiConsole.MarkupLine("[red]Error:[/] No .md instructions file found in the input folder.");
    return 1;
}

// ── Build Host ────────────────────────────────────────────────────────────────
var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((ctx, config) =>
    {
        config.SetBasePath(AppContext.BaseDirectory);
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
        config.AddJsonFile($"appsettings.{ctx.HostingEnvironment.EnvironmentName}.json", optional: true);
        config.AddEnvironmentVariables();
    })
    .ConfigureServices((ctx, services) =>
    {
        var configuration = ctx.Configuration;

        // Core services
        services.AddSingleton<IXmlToJsonConverter, XmlToJsonConverter>();
        services.AddSingleton<IJsonEncoderService, JsonEncoderService>();
        services.AddSingleton<IXsdValidator, XsdValidator>();
        services.AddSingleton<ICrystalReportExporter, CrystalReportExporter>();

        // AI services (Semantic Kernel)
        services.AddSemanticKernel(configuration);
        services.AddSingleton<IXmlGeneratorService, XmlGeneratorService>();
        services.AddSingleton<IBlazorComponentGenerator, BlazorComponentGenerator>();

        // Blazor validation
        services.AddSingleton<RazorValidator>();

        // Orchestrator
        services.AddSingleton<ReportPipelineOrchestrator>();
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
    })
    .Build();

// ── Resolve services and run pipeline ────────────────────────────────────────
var configuration = host.Services.GetRequiredService<IConfiguration>();
var orchestrator = host.Services.GetRequiredService<ReportPipelineOrchestrator>();

var outputPath = configuration["Reports:OutputPath"] ?? "./output";
if (!Path.IsPathRooted(outputPath))
    outputPath = Path.GetFullPath(Path.Combine(inputFolder, outputPath));

var maxRetries = int.TryParse(configuration["Pipeline:MaxAIRetries"], out var r) ? r : 3;

var input = new PipelineInput
{
    InputFolderPath = inputFolder,
    XsdFilePath = xsdFiles[0],
    RptFilePath = rptFiles[0],
    MarkdownFilePath = mdFiles[0],
    OutputFolderPath = outputPath
};

AnsiConsole.Write(
    new FigletText("XmlReportGenerator")
        .LeftJustified()
        .Color(Color.Aqua));

AnsiConsole.MarkupLine($"[bold]Input folder:[/] {inputFolder}");
AnsiConsole.MarkupLine($"[bold]XSD:[/] {Path.GetFileName(input.XsdFilePath)}");
AnsiConsole.MarkupLine($"[bold]RPT:[/] {Path.GetFileName(input.RptFilePath)}");
AnsiConsole.MarkupLine($"[bold]Instructions:[/] {Path.GetFileName(input.MarkdownFilePath)}");
AnsiConsole.MarkupLine($"[bold]Output:[/] {outputPath}");
AnsiConsole.WriteLine();

PipelineResult result = default!;

await AnsiConsole.Progress()
    .StartAsync(async ctx =>
    {
        var task = ctx.AddTask("[green]Running pipeline[/]", maxValue: 4);
        result = await orchestrator.RunAsync(input, maxRetries);
        task.Value = 4;
    });

if (result.Success)
{
    AnsiConsole.MarkupLine("[green]✓ Pipeline completed successfully.[/]");
    AnsiConsole.MarkupLine($"  Generated XML length : {result.GeneratedXml.Length} chars");
    AnsiConsole.MarkupLine($"  JSON wrapper         : {Path.Combine(outputPath, "payload.json")}");
    AnsiConsole.MarkupLine($"  Report HTML          : {Path.Combine(outputPath, "report.html")}");
    AnsiConsole.MarkupLine($"  Blazor component     : {Path.Combine(outputPath, "GeneratedReport.razor")}");
    return 0;
}
else
{
    AnsiConsole.MarkupLine($"[red]✗ Pipeline failed:[/] {result.ErrorMessage}");
    return 1;
}
