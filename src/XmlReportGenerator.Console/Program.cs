using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Spectre.Console;
using XmlReportGenerator.Core.Models;
using XmlReportGenerator.AI.Extensions;
using XmlReportGenerator.AI.Services;
using XmlReportGenerator.Blazor.Services;
using XmlReportGenerator.Core.Interfaces;
using XmlReportGenerator.Core.Orchestrator;
using XmlReportGenerator.Core.Services;
using XmlReportGenerator.Reports.Services;

// ── Parse arguments ────────────────────────────────────────────────────────────
string inputFolder = string.Empty;
for (int i = 0; i < args.Length; i++)
{
    if ((args[i] == "--input" || args[i] == "-i") && i + 1 < args.Length)
    {
        inputFolder = args[i + 1];
        break;
    }
}

if (string.IsNullOrWhiteSpace(inputFolder))
{
    // If no argument provided, use the current directory
    inputFolder = Directory.GetCurrentDirectory();
}

inputFolder = Path.GetFullPath(inputFolder);

AnsiConsole.Write(
    new FigletText("XmlReportGenerator")
        .Centered()
        .Color(Color.Cyan1));

AnsiConsole.MarkupLine($"[bold]Input folder:[/] [yellow]{inputFolder}[/]");

if (!Directory.Exists(inputFolder))
{
    AnsiConsole.MarkupLine("[red]Error:[/] The specified input folder does not exist.");
    return 1;
}

// ── Find input files ───────────────────────────────────────────────────────────
var rptFile = Directory.GetFiles(inputFolder, "*.rpt").FirstOrDefault();
var xsdFile = Directory.GetFiles(inputFolder, "*.xsd").FirstOrDefault();
var mdFile  = Directory.GetFiles(inputFolder, "*.md").FirstOrDefault();

if (rptFile is null)
{
    AnsiConsole.MarkupLine("[red]Error:[/] No .rpt file found in the input folder.");
    return 1;
}
if (xsdFile is null)
{
    AnsiConsole.MarkupLine("[red]Error:[/] No .xsd file found in the input folder.");
    return 1;
}
if (mdFile is null)
{
    AnsiConsole.MarkupLine("[red]Error:[/] No .md instructions file found in the input folder.");
    return 1;
}

AnsiConsole.MarkupLine($"  [green]✓[/] RPT:  [dim]{rptFile}[/]");
AnsiConsole.MarkupLine($"  [green]✓[/] XSD:  [dim]{xsdFile}[/]");
AnsiConsole.MarkupLine($"  [green]✓[/] MD:   [dim]{mdFile}[/]");

// ── Build host ─────────────────────────────────────────────────────────────────
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Core services
        services.AddSingleton<XmlReportGenerator.Core.Interfaces.IXsdValidator, XsdValidator>();
        services.AddSingleton<IXmlToJsonConverter, XmlToJsonConverter>();
        services.AddSingleton<IJsonEncoderService, JsonEncoderService>();

        // AI services
        services.AddAiServices(context.Configuration);
        services.AddSingleton<IXmlGeneratorService, XmlGeneratorService>();
        services.AddSingleton<IBlazorComponentGenerator, BlazorComponentGenerator>();

        // Reports
        services.AddSingleton<ICrystalReportExporter, CrystalReportExporter>();

        // Blazor
        services.AddSingleton<RazorValidator>();

        // Orchestrator
        services.AddSingleton<ReportPipelineOrchestrator>();
    })
    .Build();

// ── Run pipeline ───────────────────────────────────────────────────────────────
var orchestrator = host.Services.GetRequiredService<ReportPipelineOrchestrator>();
var outputDir = Path.Combine(inputFolder, "output");

var input = new XmlReportGenerator.Core.Models.PipelineInput
{
    RptFilePath      = rptFile,
    XsdFilePath      = xsdFile,
    MarkdownFilePath = mdFile,
    OutputDirectory  = outputDir,
};

PipelineResult? result = null;

await AnsiConsole.Progress()
    .AutoRefresh(true)
    .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new SpinnerColumn())
    .StartAsync(async ctx =>
    {
        var task = ctx.AddTask("[bold]Running pipeline[/]", maxValue: 1);
        result = await orchestrator.RunAsync(input);
        task.Increment(1);
    });

// ── Display results ────────────────────────────────────────────────────────────
if (result is null || !result.IsSuccess)
{
    AnsiConsole.MarkupLine("[red]Pipeline failed![/]");
    foreach (var error in result?.Errors ?? [])
        AnsiConsole.MarkupLine($"  [red]✗[/] {error}");
    return 1;
}

AnsiConsole.MarkupLine("[green]Pipeline completed successfully![/]");
AnsiConsole.MarkupLine($"  [dim]Output directory:[/] {outputDir}");
AnsiConsole.MarkupLine($"  [dim]HTML report:[/]     {result.HtmlReportPath}");
return 0;
