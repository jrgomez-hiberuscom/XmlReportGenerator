using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;

namespace CrystalReportExporter.Tool;

/// <summary>
/// Command-line tool that exports a Crystal Reports .rpt file to HTML using XML data.
/// Usage: CrystalReportExporter.Tool.exe --rpt "path.rpt" --xml "path.xml" --output "path.html"
/// </summary>
internal class Program
{
    static int Main(string[] args)
    {
        try
        {
            string rptPath = "C:\\Proyectos\\XmlReportGenerator\\tools\\CrystalReportExporter.Tool\\Original\\ImpresoSolicitud_IOEDLP.rpt";
            string xmlPath = "C:\\Proyectos\\XmlReportGenerator\\tools\\CrystalReportExporter.Tool\\Original\\InformeSolicitudIOEDLPDS.xml";
            string xsdPath = "C:\\Proyectos\\XmlReportGenerator\\tools\\CrystalReportExporter.Tool\\Original\\InformeSolicitudIOEDLPDS.xsd";
            string outputPath = "C:\\Proyectos\\XmlReportGenerator\\tools\\CrystalReportExporter.Tool\\Original\\report.html";

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLowerInvariant())
                {
                    case "--rpt" when i + 1 < args.Length:
                        rptPath = args[++i];
                        break;
                    case "--xml" when i + 1 < args.Length:
                        xmlPath = args[++i];
                        break;
                    case "--xsd" when i + 1 < args.Length:
                        xsdPath = args[++i];
                        break;
                    case "--output" when i + 1 < args.Length:
                        outputPath = args[++i];
                        break;
                }
            }

            if (string.IsNullOrEmpty(rptPath) || string.IsNullOrEmpty(xmlPath) || string.IsNullOrEmpty(outputPath))
            {
                Console.Error.WriteLine("Usage: CrystalReportExporter.Tool.exe --rpt <file.rpt> --xml <file.xml> --output <file.html>");
                return 1;
            }

            if (!File.Exists(rptPath))
            {
                Console.Error.WriteLine($"RPT file not found: {rptPath}");
                return 2;
            }

            if (!File.Exists(xmlPath))
            {
                Console.Error.WriteLine($"XML file not found: {xmlPath}");
                return 2;
            }

            if (!File.Exists(xsdPath))
            {
                Console.Error.WriteLine($"XSD file not found: {xsdPath}");
                return 2;
            }

            ExportToHtml(rptPath, xmlPath, xsdPath, outputPath);
            Console.WriteLine($"OK: {outputPath}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERROR: {ex.GetType().Name}: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            return 3;
        }
    }

    private static void ExportToHtml(string rptPath, string xmlPath, string xsdPath, string outputPath)
    {
        var reporte = new ReportDocument();
        string carpetaExport = Path.Combine(Path.GetTempPath(), "CRExport_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(carpetaExport);

        try
        {
            reporte.Load(rptPath);

            // Load XML into DataSet
            var ds = new DataSet();
            ds.ReadXmlSchema(xsdPath); 
            ds.ReadXml(xmlPath, XmlReadMode.IgnoreSchema);

            // Reset connections
            ResetearDatasets(reporte, ds);
            reporte.SetDataSource(ds);

            // Configure HTML export
            var htmlOpciones = new HTMLFormatOptions();
            htmlOpciones.UsePageRange = false;
            htmlOpciones.HTMLFileName = Path.Combine(carpetaExport, "report.html");
            htmlOpciones.HTMLBaseFolderName = carpetaExport;

            var exportOpciones = reporte.ExportOptions;
            exportOpciones.ExportFormatType = ExportFormatType.HTML40;
            exportOpciones.ExportFormatOptions = htmlOpciones;
            exportOpciones.ExportDestinationType = ExportDestinationType.DiskFile;

            string errorExport = null;
            try
            {
                reporte.Export();
            }
            catch (Exception ex)
            {
                errorExport = ex.GetType().Name + ": " + ex.Message;
            }

            var fileName = Path.GetFileNameWithoutExtension(rptPath);
            var rutaExportada = Path.Combine(carpetaExport, fileName);

            // Collect HTML files
            var htmlFiles = (Directory.Exists(rutaExportada)
                    ? Directory.GetFiles(rutaExportada, "*.htm")
                    : Array.Empty<string>())
                .Concat(Directory.GetFiles(carpetaExport, "*.html"))
                .OrderBy(f => f)
                .ToList();

            if (htmlFiles.Count == 0)
            {
                var todos = Directory.GetFiles(carpetaExport);
                var diagnostico = todos.Length == 0
                    ? "(empty folder)"
                    : string.Join(", ", todos.Select(Path.GetFileName));
                throw new FileNotFoundException(
                    "Crystal Reports did not generate any HTML in: " + carpetaExport +
                    " | Files: " + diagnostico +
                    (errorExport != null ? " | Export error: " + errorExport : ""));
            }

            // Concatenate pages and embed images as data URIs
            var sb = new StringBuilder();
            foreach (var fichero in htmlFiles)
            {
                var contenido = File.ReadAllText(fichero, Encoding.UTF8);
                contenido = EmbedImagenesComoDataUri(contenido, Directory.Exists(rutaExportada) ? rutaExportada : carpetaExport);
                sb.AppendLine(contenido);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
        }
        finally
        {
            reporte.Close();
            reporte.Dispose();
            try { Directory.Delete(carpetaExport, true); } catch { }
        }
    }

    private static void ResetearDatasets(ReportDocument reporte, DataSet ds)
    {
        foreach (Table tabla in reporte.Database.Tables)
        {
            var logOnInfo = tabla.LogOnInfo;
            logOnInfo.ConnectionInfo = new ConnectionInfo
            {
                Type = ConnectionInfoType.CRQE,
                ServerName = string.Empty,
                DatabaseName = string.Empty,
                UserID = string.Empty,
                Password = string.Empty
            };
            tabla.ApplyLogOnInfo(logOnInfo);
        }

        foreach (ReportDocument subReporte in reporte.Subreports)
        {
            foreach (Table tabla in subReporte.Database.Tables)
            {
                var logOnInfo = tabla.LogOnInfo;
                logOnInfo.ConnectionInfo = new ConnectionInfo
                {
                    Type = ConnectionInfoType.CRQE,
                    ServerName = string.Empty,
                    DatabaseName = string.Empty,
                    UserID = string.Empty,
                    Password = string.Empty
                };
                tabla.ApplyLogOnInfo(logOnInfo);
            }
            subReporte.SetDataSource(ds);
        }
    }

    private static string EmbedImagenesComoDataUri(string html, string carpetaBase)
    {
        return Regex.Replace(
            html,
            @"(src\s*=\s*[""'])([^""']+)([""'])",
            m =>
            {
                string atributo = m.Groups[1].Value;
                string src = m.Groups[2].Value;
                string cierre = m.Groups[3].Value;

                if (src.StartsWith("data:", StringComparison.OrdinalIgnoreCase) ||
                    src.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    return m.Value;

                string rutaImagen = Path.IsPathRooted(src)
                    ? src
                    : Path.Combine(carpetaBase, src.Replace('/', Path.DirectorySeparatorChar));

                if (!File.Exists(rutaImagen))
                    return m.Value;

                string mimeType = ObtenerMimeType(rutaImagen);
                string base64 = Convert.ToBase64String(File.ReadAllBytes(rutaImagen));
                return atributo + "data:" + mimeType + ";base64," + base64 + cierre;
            },
            RegexOptions.IgnoreCase);
    }

    private static string ObtenerMimeType(string rutaFichero)
    {
        switch (Path.GetExtension(rutaFichero).ToLowerInvariant())
        {
            case ".png": return "image/png";
            case ".jpg":
            case ".jpeg": return "image/jpeg";
            case ".gif": return "image/gif";
            case ".bmp": return "image/bmp";
            case ".svg": return "image/svg+xml";
            default: return "image/png";
        }
    }
}
