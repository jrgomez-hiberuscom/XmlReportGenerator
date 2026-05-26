# XmlReportGenerator

A .NET 9 console application that orchestrates a four-step pipeline using AI, XML processing, Crystal Reports, and Blazor component generation.

---

## Prerequisites

| Requirement | Details |
|---|---|
| **.NET 9 SDK** | Download from https://dotnet.microsoft.com/download/dotnet/9 |
| **SAP Crystal Reports Runtime** | Install the **SAP Crystal Reports Runtime SP for Visual Studio** (x86) from https://www.sap.com/products/technology-platform/crystal-reports.html. Required for `XmlReportGenerator.Reports`. |
| **LLM API Keys** | At least one of: OpenAI, Azure OpenAI, or Google AI (Gemini). Configured in `appsettings.json`. |

---

## Solution Structure

```
XmlReportGenerator/
├── src/
│   ├── XmlReportGenerator.Console/   # Entry point (console app)
│   ├── XmlReportGenerator.Core/      # Domain logic, interfaces, pipeline orchestrator
│   ├── XmlReportGenerator.AI/        # Semantic Kernel integration (XML + Blazor generation)
│   ├── XmlReportGenerator.Reports/   # Crystal Reports export (requires SAP runtime)
│   └── XmlReportGenerator.Blazor/    # Razor component validation (Roslyn)
└── tests/
    ├── XmlReportGenerator.Core.Tests/
    └── XmlReportGenerator.AI.Tests/
```

---

## How the Pipeline Works

Given a folder containing:
- `schema.xsd` — XML Schema Definition
- `report.rpt` — Crystal Reports template
- `instructions.md` — Natural language description of desired data

The application performs four steps:

1. **AI XML Generation** — Uses Semantic Kernel + LLM to read the `.xsd` and `.md` and generate a fictitious but schema-valid XML document.
2. **XML Pipeline** — Converts XML → JSON → Base64 → JSON wrapper (all in code, no AI).
3. **Crystal Reports Export** — Loads the `.rpt`, injects the generated XML as a datasource, and exports to HTML.
4. **Blazor Component Generator** — Uses Semantic Kernel + LLM to generate a dynamic `.razor` component that reproduces the same HTML from the XML.

---

## Configuration

Copy `appsettings.json` and fill in your credentials:

```json
{
  "AI": {
    "DefaultProvider": "OpenAI",
    "OpenAI": {
      "ApiKey": "YOUR_OPENAI_KEY",
      "Model": "gpt-4o"
    },
    "AzureOpenAI": {
      "Endpoint": "https://YOUR_RESOURCE.openai.azure.com/",
      "ApiKey": "YOUR_AZURE_OPENAI_KEY",
      "DeploymentName": "gpt-4o"
    },
    "Google": {
      "ApiKey": "YOUR_GOOGLE_AI_KEY",
      "Model": "gemini-1.5-pro"
    },
    "Anthropic": {
      "ApiKey": "YOUR_ANTHROPIC_KEY",
      "Model": "claude-3-5-sonnet-20241022"
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

> ⚠️ **Never commit API keys.** The `.gitignore` already excludes `appsettings.Development.json`.

---

## How to Run

```bash
# Restore and build
dotnet build XmlReportGenerator.sln

# Run pointing to your input folder
dotnet run --project src/XmlReportGenerator.Console -- --input ./my-folder

# Run tests
dotnet test XmlReportGenerator.sln
```

The `--input` argument must point to a folder containing exactly one `.xsd`, one `.rpt`, and one `.md` file.

---

## Crystal Reports Note

The `XmlReportGenerator.Reports` project uses `#if CRYSTAL_REPORTS` guards around the real Crystal Reports code. To enable it:

1. Install the **SAP Crystal Reports Runtime SP** (x86) from SAP's website.
2. Add references to the DLLs in `XmlReportGenerator.Reports.csproj`:
   ```xml
   <ItemGroup Condition="$(DefineConstants.Contains('CRYSTAL_REPORTS'))">
     <Reference Include="CrystalDecisions.CrystalReports.Engine">
       <HintPath>C:\Program Files (x86)\SAP BusinessObjects\Crystal Reports for .NET Framework 4.0\Common\SAP BusinessObjects Enterprise XI 4.0\win32_x86\dotnet4\CrystalDecisions.CrystalReports.Engine.dll</HintPath>
     </Reference>
     <!-- other Crystal Reports DLLs -->
   </ItemGroup>
   ```
3. Build with the `CRYSTAL_REPORTS` constant defined.
