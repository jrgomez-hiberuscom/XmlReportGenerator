# XmlReportGenerator

A .NET 9 console application that, given a folder containing a Crystal Reports `.rpt`, an XSD schema `.xsd`, and a Markdown instructions `.md` file, performs these four automated steps:

1. **AI XML Generation** — Uses Semantic Kernel + LLM to read the `.xsd` and `.md` and generate a fictitious but schema-valid XML document.
2. **XML Pipeline** — Converts XML → JSON → Base64 → predefined JSON wrapper (pure code, no AI).
3. **Crystal Reports Export** — Loads the `.rpt`, injects the generated XML as the datasource, and exports the report to HTML.
4. **Blazor Component Generator** — Uses Semantic Kernel + LLM to generate a dynamic `.razor` component that reproduces the same HTML from the XML.

---

## Solution Structure

```
XmlReportGenerator/
├── XmlReportGenerator.sln
├── global.json                         # Fixes .NET 9 SDK
├── Directory.Build.props               # Common build properties
├── Directory.Packages.props            # Central Package Management
├── src/
│   ├── XmlReportGenerator.Console/     # Entry point (Exe)
│   ├── XmlReportGenerator.Core/        # Interfaces, models, services, orchestrator
│   ├── XmlReportGenerator.AI/          # Semantic Kernel integration
│   ├── XmlReportGenerator.Reports/     # Crystal Reports exporter
│   └── XmlReportGenerator.Blazor/      # Razor component validator
└── tests/
    ├── XmlReportGenerator.Core.Tests/
    └── XmlReportGenerator.AI.Tests/
```

---

## Prerequisites

### Required

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9) (9.0 or later)
- A valid API key for at least one of the supported LLM providers (see configuration below)

### Required for Crystal Reports export (Step 3)

- **SAP Crystal Reports Runtime for Visual Studio** — This is **NOT available on NuGet**. You must download and install it from:
  [SAP Crystal Reports Runtime](https://www.sap.com/products/technology-platform/crystal-reports.html)
- After installation, DLLs are typically located at:
  ```
  C:\Program Files (x86)\SAP BusinessObjects\Crystal Reports for .NET Framework 4.0\
  Common\SAP BusinessObjects Enterprise XI 4.0\win32_x86\
  ```
- See `src/XmlReportGenerator.Reports/XmlReportGenerator.Reports.csproj` for instructions on adding local DLL references and enabling the `CRYSTAL_REPORTS` compilation constant.
- Without the runtime, the application will produce a **placeholder HTML stub** and continue with Step 4.

---

## Configuration

Edit `src/XmlReportGenerator.Console/appsettings.json` to configure your LLM provider:

```json
{
  "AI": {
    "DefaultProvider": "OpenAI",
    "OpenAI": {
      "ApiKey": "YOUR_OPENAI_API_KEY",
      "Model": "gpt-4o"
    },
    "AzureOpenAI": {
      "Endpoint": "https://YOUR_RESOURCE.openai.azure.com/",
      "ApiKey": "YOUR_AZURE_OPENAI_API_KEY",
      "DeploymentName": "gpt-4o"
    },
    "Google": {
      "ApiKey": "YOUR_GOOGLE_API_KEY",
      "Model": "gemini-1.5-pro"
    }
  },
  "Reports": {
    "OutputPath": "./output",
    "ExportFormat": "HTML"
  },
  "Pipeline": {
    "ValidateXmlAfterGeneration": true,
    "MaxAIRetries": 3
  }
}
```

**Supported `DefaultProvider` values:** `OpenAI`, `AzureOpenAI`, `Google`

---

## How to Run

1. **Restore and build:**
   ```bash
   dotnet restore
   dotnet build
   ```

2. **Prepare your input folder** — place the following files in a folder:
   ```
   my-folder/
   ├── report.rpt          # Your Crystal Reports file
   ├── schema.xsd          # The XSD schema for the report datasource
   └── instructions.md     # Markdown file describing what data to generate
   ```

3. **Run the application:**
   ```bash
   dotnet run --project src/XmlReportGenerator.Console -- --input ./my-folder
   ```
   Or if you omit `--input`, the current working directory is used:
   ```bash
   cd my-folder
   dotnet run --project /path/to/XmlReportGenerator.Console
   ```

4. **Output** — all generated files are placed in `my-folder/output/`:
   ```
   my-folder/output/
   ├── generated.xml           # AI-generated XML (Step 1)
   ├── payload.json            # JSON wrapper with Base64 payload (Step 2)
   ├── report.html             # Crystal Reports HTML export (Step 3)
   └── ReportComponent.razor   # Generated Blazor component (Step 4)
   ```

---

## Running Tests

```bash
dotnet test
```

---

## Architecture Notes

- **Central Package Management** — All NuGet package versions are defined in `Directory.Packages.props`.
- **Crystal Reports stub** — When the `CRYSTAL_REPORTS` compile constant is not defined, `CrystalReportExporter` produces a placeholder HTML file so the pipeline can complete without the runtime.
- **Semantic Kernel** — `SemanticKernelExtensions.AddSemanticKernel()` reads `AI:DefaultProvider` from configuration and wires up the appropriate LLM connector.
- **Prompt templates** — SK prompt YAML files are in `src/XmlReportGenerator.AI/Prompts/`. Inline fallback prompts are used if the YAML files are not found.
